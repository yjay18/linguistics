using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Linguistics.Core.Providers;

namespace Linguistics.App.LocalAI;

public sealed class OllamaProvider : ILanguageModelProvider, IDisposable
{
    public static readonly Uri DefaultEndpoint = new("http://localhost:11434/");

    private const int MaximumResponseBytes = 262_144;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly HttpClient _client;
    private readonly Uri _endpoint;
    private readonly TimeSpan _timeout;
    private readonly bool _ownsClient;
    private readonly ConcurrentDictionary<Guid, Guid> _latestRequests = new();

    public OllamaProvider(
        HttpClient client,
        Uri endpoint,
        TimeSpan? timeout = null)
        : this(client, endpoint, timeout, ownsClient: false)
    {
    }

    private OllamaProvider(
        HttpClient client,
        Uri endpoint,
        TimeSpan? timeout,
        bool ownsClient)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _endpoint = ValidateLocalEndpoint(endpoint);
        _timeout = timeout ?? TimeSpan.FromSeconds(20);
        if (_timeout <= TimeSpan.Zero || _timeout > TimeSpan.FromMinutes(2))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        _ownsClient = ownsClient;
    }

    public static OllamaProvider CreateDefault()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
        };
        return new OllamaProvider(new HttpClient(handler), DefaultEndpoint, null, ownsClient: true);
    }

    public static Uri ValidateLocalEndpoint(Uri endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        if (!endpoint.IsAbsoluteUri ||
            endpoint.Scheme != Uri.UriSchemeHttp ||
            !endpoint.IsLoopback ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) ||
            !string.IsNullOrEmpty(endpoint.Fragment) ||
            endpoint.AbsolutePath != "/")
        {
            throw new ArgumentException(
                "The default product mode accepts only a plain HTTP loopback Ollama endpoint.",
                nameof(endpoint));
        }

        return endpoint;
    }

    public static bool IsCloudAlias(string model) =>
        !string.IsNullOrWhiteSpace(model) &&
        model.Trim().EndsWith("-cloud", StringComparison.OrdinalIgnoreCase);

    public async Task<LocalModelServiceSnapshot> InspectServiceAsync(
        CancellationToken cancellationToken = default)
    {
        using var timeout = CreateTimeout(cancellationToken);
        try
        {
            var versionResponse = await SendAsync(
                HttpMethod.Get,
                "api/version",
                body: null,
                timeout.Token).ConfigureAwait(false);
            if (!versionResponse.Success)
            {
                return UnavailableSnapshot(versionResponse.Message);
            }

            var version = ReadVersion(versionResponse.Body);
            var tagsResponse = await SendAsync(
                HttpMethod.Get,
                "api/tags",
                body: null,
                timeout.Token).ConfigureAwait(false);
            if (!tagsResponse.Success)
            {
                return UnavailableSnapshot(tagsResponse.Message);
            }

            var models = ReadModels(tagsResponse.Body);
            var localCount = models.Count(model => !model.IsCloudAlias);
            var cloudCount = models.Count - localCount;
            return new LocalModelServiceSnapshot(
                LocalModelServiceStatus.Available,
                version,
                models,
                cloudCount == 0
                    ? $"Ollama {version} is available with {localCount} installed local model(s)."
                    : $"Ollama {version} is available with {localCount} local model(s); {cloudCount} cloud alias(es) are blocked.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return UnavailableSnapshot("The local Ollama check timed out.");
        }
        catch (HttpRequestException)
        {
            return UnavailableSnapshot("Local Ollama is not available; scripted practice remains available.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return new LocalModelServiceSnapshot(
                LocalModelServiceStatus.InvalidResponse,
                null,
                [],
                "The local Ollama service returned metadata that could not be validated.");
        }
    }

    public async Task<LocalModelDetails> InspectModelAsync(
        string model,
        CancellationToken cancellationToken = default)
    {
        ValidateModelName(model);
        if (IsCloudAlias(model))
        {
            return new LocalModelDetails(
                LocalModelServiceStatus.InvalidResponse,
                model,
                string.Empty,
                [],
                "Cloud model aliases are blocked in local-only mode.");
        }

        using var timeout = CreateTimeout(cancellationToken);
        try
        {
            var response = await SendAsync(
                HttpMethod.Post,
                "api/show",
                JsonSerializer.Serialize(new { model }, JsonOptions),
                timeout.Token).ConfigureAwait(false);
            if (!response.Success)
            {
                return new LocalModelDetails(
                    LocalModelServiceStatus.Unavailable,
                    model,
                    string.Empty,
                    [],
                    response.Message);
            }

            using var document = JsonDocument.Parse(response.Body);
            var root = RequireObject(document.RootElement);
            var license = OptionalLicenseText(root, "license");
            var capabilities = OptionalStringArray(root, "capabilities");
            return new LocalModelDetails(
                LocalModelServiceStatus.Available,
                model,
                license,
                capabilities,
                string.IsNullOrWhiteSpace(license)
                    ? "The model is installed locally, but Ollama did not report license text. Compatibility and redistribution are unverified."
                    : "The model is installed locally. Its reported license still requires project review before recommendation or redistribution.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new LocalModelDetails(
                LocalModelServiceStatus.Unavailable,
                model,
                string.Empty,
                [],
                "The local model inspection timed out.");
        }
        catch (HttpRequestException)
        {
            return new LocalModelDetails(
                LocalModelServiceStatus.Unavailable,
                model,
                string.Empty,
                [],
                "Local Ollama is not available.");
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return new LocalModelDetails(
                LocalModelServiceStatus.InvalidResponse,
                model,
                string.Empty,
                [],
                "Ollama returned model details that could not be validated.");
        }
    }

    public async Task<DialogueGenerationResult> GenerateDialogueAsync(
        DialogueGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        DialogueProposalValidator.ValidateRequest(request);
        var stopwatch = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(request.SelectedModel))
        {
            return Result(
                request,
                LanguageModelResultStatus.NoModelSelected,
                null,
                "no-model",
                "No local conversation model is selected; using the scripted response.",
                stopwatch.Elapsed);
        }

        if (!IsValidModelName(request.SelectedModel))
        {
            return Result(
                request,
                LanguageModelResultStatus.InvalidResponse,
                null,
                "model.invalid",
                "The selected local model identifier is invalid; using the scripted response.",
                stopwatch.Elapsed);
        }

        if (IsCloudAlias(request.SelectedModel))
        {
            return Result(
                request,
                LanguageModelResultStatus.InvalidResponse,
                null,
                "cloud-model-blocked",
                "Cloud model aliases are blocked; using the scripted response.",
                stopwatch.Elapsed);
        }

        _latestRequests[request.SessionId] = request.RequestId;
        using var timeout = CreateTimeout(cancellationToken);
        try
        {
            var response = await SendAsync(
                HttpMethod.Post,
                "api/chat",
                BuildChatRequest(request),
                timeout.Token).ConfigureAwait(false);
            if (!IsLatest(request))
            {
                return Result(
                    request,
                    LanguageModelResultStatus.Stale,
                    null,
                    "stale",
                    "An obsolete local-model response was discarded.",
                    stopwatch.Elapsed);
            }

            if (!response.Success)
            {
                return Result(
                    request,
                    LanguageModelResultStatus.Unavailable,
                    null,
                    "transport",
                    "The local model did not respond successfully; using the scripted response.",
                    stopwatch.Elapsed);
            }

            var proposalJson = ReadChatContent(response.Body);
            var validation = DialogueProposalValidator.Validate(request, proposalJson);
            if (!IsLatest(request))
            {
                return Result(
                    request,
                    LanguageModelResultStatus.Stale,
                    null,
                    "stale",
                    "An obsolete local-model response was discarded.",
                    stopwatch.Elapsed);
            }

            return validation.IsValid
                ? Result(
                    request,
                    LanguageModelResultStatus.Accepted,
                    validation.Proposal,
                    validation.Code,
                    "The bounded local-model proposal passed schema and allow-list validation.",
                    stopwatch.Elapsed)
                : Result(
                    request,
                    LanguageModelResultStatus.InvalidResponse,
                    null,
                    validation.Code,
                    "The local-model proposal was rejected; using the scripted response.",
                    stopwatch.Elapsed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result(
                request,
                LanguageModelResultStatus.Cancelled,
                null,
                "cancelled",
                "The local-model request was cancelled; no proposal was applied.",
                stopwatch.Elapsed);
        }
        catch (OperationCanceledException)
        {
            return Result(
                request,
                LanguageModelResultStatus.TimedOut,
                null,
                "timeout",
                "The local-model request timed out; using the scripted response.",
                stopwatch.Elapsed);
        }
        catch (HttpRequestException)
        {
            return Result(
                request,
                LanguageModelResultStatus.Unavailable,
                null,
                "unavailable",
                "Local Ollama is unavailable; using the scripted response.",
                stopwatch.Elapsed);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Result(
                request,
                LanguageModelResultStatus.InvalidResponse,
                null,
                "envelope.invalid",
                "The local-model response envelope was invalid; using the scripted response.",
                stopwatch.Elapsed);
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    private string BuildChatRequest(DialogueGenerationRequest request)
    {
        var vocabularyIds = request.AllowedVocabulary.Keys
            .Order(StringComparer.Ordinal)
            .ToArray();
        var schema = new
        {
            type = "object",
            additionalProperties = false,
            properties = new
            {
                npcResponse = new { type = "string", @enum = request.AllowedNpcResponses },
                intent = new { type = "string", @enum = request.AllowedIntents },
                taskStateProposal = new { type = "string", @enum = request.AllowedNextStates },
                usedVocabulary = new
                {
                    type = "array",
                    uniqueItems = true,
                    maxItems = 16,
                    items = new { type = "string", @enum = vocabularyIds },
                },
            },
            required = new[] { "npcResponse", "intent", "taskStateProposal", "usedVocabulary" },
        };
        var context = new
        {
            promptVersion = DialogueProposalValidator.PromptVersion,
            schemaVersion = DialogueProposalValidator.SchemaVersion,
            npcRole = request.NpcRole,
            goal = request.Goal,
            currentState = request.CurrentState,
            allowedIntents = request.AllowedIntents,
            allowedNextStates = request.AllowedNextStates,
            allowedVocabulary = request.AllowedVocabulary
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            allowedNpcResponses = request.AllowedNpcResponses,
            establishedFacts = request.EstablishedFacts,
            learnerUtterance = request.LearnerUtterance,
        };
        var body = new
        {
            model = request.SelectedModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "Select one allowed NPC response, intent, state, and vocabulary set. " +
                              "Never invent identifiers, teaching content, facts, or transitions. Return only schema JSON.",
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(context, JsonOptions),
                },
            },
            stream = false,
            think = false,
            format = schema,
            options = new
            {
                temperature = 0,
                seed = 0,
                num_predict = 192,
            },
        };
        return JsonSerializer.Serialize(body, JsonOptions);
    }

    private async Task<HttpResponse> SendAsync(
        HttpMethod method,
        string relativePath,
        string? body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(_endpoint, relativePath));
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        using var response = await _client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new HttpResponse(
                false,
                string.Empty,
                $"Local Ollama returned HTTP {(int)response.StatusCode}.");
        }

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength > MaximumResponseBytes)
        {
            throw new InvalidDataException("The local Ollama response exceeded the supported size.");
        }

        var text = await ReadBoundedAsync(response.Content, cancellationToken).ConfigureAwait(false);
        return new HttpResponse(true, text, string.Empty);
    }

    private static async Task<string> ReadBoundedAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(8_192);
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                if (output.Length + read > MaximumResponseBytes)
                {
                    throw new InvalidDataException("The local Ollama response exceeded the supported size.");
                }

                output.Write(buffer, 0, read);
            }

            return Encoding.UTF8.GetString(output.GetBuffer(), 0, checked((int)output.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string ReadVersion(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement);
        var version = RequiredString(root, "version");
        if (version.Length > 64)
        {
            throw new InvalidDataException("The Ollama version is too long.");
        }

        return version;
    }

    private static IReadOnlyList<LocalModelSummary> ReadModels(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement);
        if (!root.TryGetProperty("models", out var modelsElement) ||
            modelsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Ollama model metadata is missing.");
        }

        var models = new List<LocalModelSummary>();
        foreach (var element in modelsElement.EnumerateArray())
        {
            var item = RequireObject(element);
            var name = RequiredString(item, "name");
            if (!IsValidModelName(name))
            {
                throw new InvalidDataException("An Ollama model name is invalid.");
            }
            var size = RequiredInt64(item, "size");
            if (size < 0)
            {
                throw new InvalidDataException("An Ollama model size is invalid.");
            }

            var modified = OptionalDate(item, "modified_at");
            var details = item.TryGetProperty("details", out var detailsElement) &&
                          detailsElement.ValueKind == JsonValueKind.Object
                ? detailsElement
                : default;
            models.Add(new LocalModelSummary(
                name,
                modified,
                size,
                OptionalString(item, "digest"),
                OptionalString(details, "format"),
                OptionalString(details, "family"),
                OptionalString(details, "parameter_size"),
                OptionalString(details, "quantization_level"),
                IsCloudAlias(name)));
        }

        if (models.Select(model => model.Name).Distinct(StringComparer.Ordinal).Count() != models.Count)
        {
            throw new InvalidDataException("Ollama returned duplicate model names.");
        }

        return models.OrderBy(model => model.Name, StringComparer.Ordinal).ToArray();
    }

    private static string ReadChatContent(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = RequireObject(document.RootElement);
        if (!root.TryGetProperty("done", out var done) || done.ValueKind != JsonValueKind.True ||
            !root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("The Ollama chat response is incomplete.");
        }

        return RequiredString(message, "content");
    }

    private static JsonElement RequireObject(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
            ? element
            : throw new InvalidDataException("An Ollama response object is missing.");

    private static string RequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidDataException($"Ollama field '{propertyName}' is missing.");
        }

        return property.GetString()!;
    }

    private static string OptionalString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static string OptionalLicenseText(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(
                Environment.NewLine,
                property.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(value => !string.IsNullOrWhiteSpace(value))),
            _ => string.Empty,
        };
    }

    private static long RequiredInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetInt64(out var value))
        {
            throw new InvalidDataException($"Ollama field '{propertyName}' is missing.");
        }

        return value;
    }

    private static DateTimeOffset? OptionalDate(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(property.GetString(), out var value)
            ? value
            : null;

    private static IReadOnlyList<string> OptionalStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return values;
    }

    private static void ValidateModelName(string model)
    {
        if (!IsValidModelName(model))
        {
            throw new ArgumentException("The Ollama model name is invalid.", nameof(model));
        }
    }

    private static bool IsValidModelName(string? model) =>
        !string.IsNullOrWhiteSpace(model) &&
        model.Length <= 200 &&
        !model.Any(char.IsControl);

    private CancellationTokenSource CreateTimeout(CancellationToken cancellationToken)
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);
        return timeout;
    }

    private bool IsLatest(DialogueGenerationRequest request) =>
        _latestRequests.TryGetValue(request.SessionId, out var latest) &&
        latest == request.RequestId;

    private static LocalModelServiceSnapshot UnavailableSnapshot(string message) =>
        new(LocalModelServiceStatus.Unavailable, null, [], message);

    private static DialogueGenerationResult Result(
        DialogueGenerationRequest request,
        LanguageModelResultStatus status,
        DialogueProposal? proposal,
        string validationResult,
        string message,
        TimeSpan duration) =>
        new(
            status,
            proposal,
            request.ScriptedFallback,
            message,
            new LanguageModelDiagnostic(
                request.SessionId,
                request.RequestId,
                IsValidModelName(request.SelectedModel) ? request.SelectedModel : null,
                duration,
                DialogueProposalValidator.PromptVersion,
                DialogueProposalValidator.SchemaVersion,
                validationResult));

    private sealed record HttpResponse(bool Success, string Body, string Message);
}
