using System.Text.Json;
using System.Text.Json.Serialization;

namespace Linguistics.Core.Providers;

public enum LocalModelServiceStatus
{
    Available,
    Unavailable,
    InvalidResponse,
}

public enum LanguageModelResultStatus
{
    Accepted,
    Unavailable,
    NoModelSelected,
    InvalidResponse,
    TimedOut,
    Cancelled,
    Stale,
}

public sealed record LocalModelSummary(
    string Name,
    DateTimeOffset? ModifiedAt,
    long SizeBytes,
    string Digest,
    string Format,
    string Family,
    string ParameterSize,
    string Quantization,
    bool IsCloudAlias);

public sealed record LocalModelServiceSnapshot(
    LocalModelServiceStatus Status,
    string? Version,
    IReadOnlyList<LocalModelSummary> Models,
    string Message);

public sealed record LocalModelDetails(
    LocalModelServiceStatus Status,
    string Model,
    string LicenseText,
    IReadOnlyList<string> Capabilities,
    string Message);

public sealed record DialogueGenerationRequest(
    Guid SessionId,
    Guid RequestId,
    string? SelectedModel,
    string NpcRole,
    string Goal,
    string CurrentState,
    IReadOnlyList<string> AllowedIntents,
    IReadOnlyList<string> AllowedNextStates,
    IReadOnlyDictionary<string, string> AllowedVocabulary,
    IReadOnlyList<string> AllowedNpcResponses,
    IReadOnlyList<string> EstablishedFacts,
    string LearnerUtterance,
    string ScriptedFallback);

public sealed record DialogueProposal(
    string NpcResponse,
    string Intent,
    string TaskStateProposal,
    IReadOnlyList<string> UsedVocabulary);

public sealed record LanguageModelDiagnostic(
    Guid SessionId,
    Guid RequestId,
    string? Model,
    TimeSpan Duration,
    string PromptVersion,
    string SchemaVersion,
    string ValidationResult);

public sealed record DialogueGenerationResult(
    LanguageModelResultStatus Status,
    DialogueProposal? Proposal,
    string ScriptedFallback,
    string Message,
    LanguageModelDiagnostic Diagnostic);

public interface ILanguageModelProvider
{
    Task<LocalModelServiceSnapshot> InspectServiceAsync(
        CancellationToken cancellationToken = default);

    Task<LocalModelDetails> InspectModelAsync(
        string model,
        CancellationToken cancellationToken = default);

    Task<DialogueGenerationResult> GenerateDialogueAsync(
        DialogueGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record DialogueProposalValidationResult(
    bool IsValid,
    DialogueProposal? Proposal,
    string Code);

public static class DialogueProposalValidator
{
    public const string PromptVersion = "cafe-dialogue-prompt-v1";
    public const string SchemaVersion = "cafe-dialogue-schema-v1";
    public const int MaximumResponseCharacters = 240;
    public const int MaximumProposalCharacters = 8_192;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    public static DialogueProposalValidationResult Validate(
        DialogueGenerationRequest request,
        string json)
    {
        ValidateRequest(request);
        if (string.IsNullOrWhiteSpace(json) || json.Length > MaximumProposalCharacters)
        {
            return Invalid("proposal.size");
        }

        DialogueProposal? proposal;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Invalid("proposal.json");
            }

            var properties = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
            var expected = new HashSet<string>(
                ["npcResponse", "intent", "taskStateProposal", "usedVocabulary"],
                StringComparer.Ordinal);
            if (properties.Length != expected.Count ||
                properties.Distinct(StringComparer.Ordinal).Count() != properties.Length ||
                properties.Any(property => !expected.Contains(property)))
            {
                return Invalid("proposal.json");
            }

            proposal = JsonSerializer.Deserialize<DialogueProposal>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return Invalid("proposal.json");
        }

        if (proposal is null ||
            string.IsNullOrWhiteSpace(proposal.NpcResponse) ||
            proposal.NpcResponse.Length > MaximumResponseCharacters ||
            !request.AllowedNpcResponses.Contains(proposal.NpcResponse, StringComparer.Ordinal))
        {
            return Invalid("proposal.npc-response");
        }

        if (!request.AllowedIntents.Contains(proposal.Intent, StringComparer.Ordinal))
        {
            return Invalid("proposal.intent");
        }

        if (!request.AllowedNextStates.Contains(proposal.TaskStateProposal, StringComparer.Ordinal))
        {
            return Invalid("proposal.transition");
        }

        if (proposal.UsedVocabulary is null ||
            proposal.UsedVocabulary.Count > 16 ||
            proposal.UsedVocabulary.Count != proposal.UsedVocabulary.Distinct(StringComparer.Ordinal).Count() ||
            proposal.UsedVocabulary.Any(id => !request.AllowedVocabulary.ContainsKey(id)))
        {
            return Invalid("proposal.vocabulary");
        }

        return new DialogueProposalValidationResult(true, proposal, "accepted");
    }

    public static void ValidateRequest(DialogueGenerationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.SessionId == Guid.Empty || request.RequestId == Guid.Empty)
        {
            throw new ArgumentException("Dialogue session and request IDs are required.", nameof(request));
        }

        RequireText(request.NpcRole, nameof(request.NpcRole));
        RequireText(request.Goal, nameof(request.Goal));
        RequireText(request.CurrentState, nameof(request.CurrentState));
        RequireText(request.LearnerUtterance, nameof(request.LearnerUtterance));
        RequireText(request.ScriptedFallback, nameof(request.ScriptedFallback));
        RequireValues(request.AllowedIntents, nameof(request.AllowedIntents));
        RequireValues(request.AllowedNextStates, nameof(request.AllowedNextStates));
        RequireValues(request.AllowedNpcResponses, nameof(request.AllowedNpcResponses));

        if (request.AllowedVocabulary is null ||
            request.AllowedVocabulary.Count == 0 ||
            request.AllowedVocabulary.Any(pair =>
                string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value)))
        {
            throw new ArgumentException("Allowed vocabulary is required.", nameof(request));
        }

        if (request.EstablishedFacts is null || request.EstablishedFacts.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Established facts cannot contain missing values.", nameof(request));
        }
    }

    private static DialogueProposalValidationResult Invalid(string code) =>
        new(false, null, code);

    private static void RequireText(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 1_000)
        {
            throw new ArgumentException($"{name} is missing or too long.", name);
        }
    }

    private static void RequireValues(IReadOnlyList<string> values, string name)
    {
        if (values is null ||
            values.Count == 0 ||
            values.Count > 32 ||
            values.Any(string.IsNullOrWhiteSpace) ||
            values.Count != values.Distinct(StringComparer.Ordinal).Count())
        {
            throw new ArgumentException($"{name} is missing, duplicated, or too large.", name);
        }
    }
}
