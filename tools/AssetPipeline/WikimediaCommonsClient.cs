using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Linguistics.AssetPipeline;

public sealed record WikimediaCandidate(
    int PageId,
    string Title,
    string Author,
    string LicenseIdentifier,
    string LicenseUrl,
    string SourceUrl,
    string FileUrl,
    string MimeType,
    int Width,
    int Height,
    long ByteSize);

public sealed record FetchedWikimediaSource(
    WikimediaCandidate Candidate,
    DateOnly RetrievedOn,
    string OriginalSha256,
    string SourceFileName);

public sealed partial class WikimediaCommonsClient : IDisposable
{
    public const string ApiEndpoint = "https://commons.wikimedia.org/w/api.php";
    public const string UserAgent =
        "LinguisticsAssetPipelineBot/0.1 (https://github.com/yjay18/linguistics; authoring-time asset research) .NET/10";

    private const long MaximumDownloadBytes = 32 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;
    private readonly SemaphoreSlim _requestGate = new(1, 1);

    public WikimediaCommonsClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
        _httpClient.DefaultRequestHeaders.UserAgent.Clear();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromSeconds(45);
    }

    public async Task<IReadOnlyList<WikimediaCandidate>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (limit is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Choose between 1 and 50 candidates.");
        }

        var uri = BuildUri(new Dictionary<string, string>
        {
            ["action"] = "query",
            ["format"] = "json",
            ["formatversion"] = "2",
            ["generator"] = "search",
            ["gsrsearch"] = $"{query} filetype:bitmap",
            ["gsrnamespace"] = "6",
            ["gsrlimit"] = limit.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["prop"] = "imageinfo",
            ["iiprop"] = "url|mime|size|extmetadata",
            ["maxlag"] = "5",
        });
        using var response = await SendWithBackoffAsync(uri, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCandidates(json);
    }

    public async Task<WikimediaCandidate> GetCandidateAsync(
        int pageId,
        CancellationToken cancellationToken = default)
    {
        if (pageId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageId));
        }

        var uri = BuildUri(new Dictionary<string, string>
        {
            ["action"] = "query",
            ["format"] = "json",
            ["formatversion"] = "2",
            ["pageids"] = pageId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["prop"] = "imageinfo",
            ["iiprop"] = "url|mime|size|extmetadata",
            ["maxlag"] = "5",
        });
        using var response = await SendWithBackoffAsync(uri, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseCandidates(json).SingleOrDefault()
            ?? throw new InvalidOperationException(
                $"Commons page {pageId} is not an allowed public-domain, CC0, CC BY, or CC BY-SA bitmap.");
    }

    public async Task<byte[]> DownloadAsync(
        WikimediaCandidate candidate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (!Uri.TryCreate(candidate.FileUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Commons returned an invalid download URL.");
        }

        using var response = await SendWithBackoffAsync(uri, cancellationToken);
        if (response.Content.Headers.ContentLength is > MaximumDownloadBytes)
        {
            throw new InvalidOperationException(
                $"The source file exceeds the {MaximumDownloadBytes}-byte authoring download limit.");
        }

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = new MemoryStream();
        var buffer = new byte[81_920];
        while (true)
        {
            var read = await input.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (output.Length + read > MaximumDownloadBytes)
            {
                throw new InvalidOperationException(
                    $"The source file exceeds the {MaximumDownloadBytes}-byte authoring download limit.");
            }

            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return output.ToArray();
    }

    public static IReadOnlyList<WikimediaCandidate> ParseCandidates(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("error", out var error))
        {
            var code = String(error, "code") ?? "unknown";
            var info = String(error, "info") ?? "Commons returned an API error.";
            throw new InvalidOperationException($"Wikimedia API error '{code}': {info}");
        }

        if (!document.RootElement.TryGetProperty("query", out var query) ||
            !query.TryGetProperty("pages", out var pages) ||
            pages.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var candidates = new List<WikimediaCandidate>();
        foreach (var page in pages.EnumerateArray())
        {
            if (!page.TryGetProperty("pageid", out var pageIdElement) ||
                !pageIdElement.TryGetInt32(out var pageId) ||
                !page.TryGetProperty("imageinfo", out var imageInfoArray) ||
                imageInfoArray.ValueKind != JsonValueKind.Array ||
                imageInfoArray.GetArrayLength() != 1)
            {
                continue;
            }

            var imageInfo = imageInfoArray[0];
            var title = Clean(String(page, "title"));
            var fileUrl = String(imageInfo, "url");
            var sourceUrl = String(imageInfo, "descriptionurl");
            var mimeType = String(imageInfo, "mime");
            if (title is null || fileUrl is null || sourceUrl is null || mimeType is null ||
                !mimeType.StartsWith("image/", StringComparison.Ordinal) ||
                !imageInfo.TryGetProperty("extmetadata", out var metadata))
            {
                continue;
            }

            var author = Metadata(metadata, "Artist") ?? Metadata(metadata, "Credit");
            var license = NormalizeLicense(Metadata(metadata, "LicenseShortName"));
            var licenseUrl = Metadata(metadata, "LicenseUrl");
            if (author is null || license is null || licenseUrl is null ||
                !Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri) ||
                fileUri.Scheme != Uri.UriSchemeHttps ||
                !Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri) ||
                sourceUri.Scheme != Uri.UriSchemeHttps ||
                !Uri.TryCreate(licenseUrl, UriKind.Absolute, out var licenseUri) ||
                licenseUri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            candidates.Add(new WikimediaCandidate(
                pageId,
                title,
                author,
                license,
                licenseUrl,
                sourceUrl,
                fileUrl,
                mimeType,
                Integer(imageInfo, "width"),
                Integer(imageInfo, "height"),
                Long(imageInfo, "size")));
        }

        return candidates
            .OrderBy(candidate => candidate.PageId)
            .ToArray();
    }

    public static string? NormalizeLicense(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = Clean(value)!
            .Replace('_', ' ')
            .Replace('-', ' ')
            .ToUpperInvariant();
        normalized = WhitespaceRegex().Replace(normalized, " ").Trim();
        if (normalized is "PUBLIC DOMAIN" or "PD")
        {
            return "Public-Domain";
        }

        if (normalized.StartsWith("CC0", StringComparison.Ordinal))
        {
            return "CC0-1.0";
        }

        var match = CreativeCommonsRegex().Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        var family = match.Groups[1].Value == "BY SA" ? "CC-BY-SA" : "CC-BY";
        var version = match.Groups[2].Value;
        return version is "1.0" or "2.0" or "2.5" or "3.0" or "4.0"
            ? $"{family}-{version}"
            : null;
    }

    public static string Sha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    public void Dispose()
    {
        _requestGate.Dispose();
        if (_ownsClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<HttpResponseMessage> SendWithBackoffAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        await _requestGate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var response = await _httpClient.GetAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                if (response.StatusCode is not HttpStatusCode.TooManyRequests and
                    not HttpStatusCode.ServiceUnavailable)
                {
                    response.EnsureSuccessStatusCode();
                    return response;
                }

                var retryAfter = response.Headers.RetryAfter?.Delta ??
                                 TimeSpan.FromSeconds(Math.Pow(2, attempt));
                response.Dispose();
                await Task.Delay(retryAfter, cancellationToken);
            }

            throw new HttpRequestException("Wikimedia kept throttling the authoring request after four attempts.");
        }
        finally
        {
            _requestGate.Release();
        }
    }

    private static Uri BuildUri(IReadOnlyDictionary<string, string> parameters)
    {
        var query = string.Join('&', parameters.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return new Uri($"{ApiEndpoint}?{query}");
    }

    private static string? Metadata(JsonElement metadata, string name) =>
        metadata.TryGetProperty(name, out var field) && field.TryGetProperty("value", out var value)
            ? Clean(value.GetString())
            : null;

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int Integer(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result)
            ? result
            : 0;

    private static long Long(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result)
            ? result
            : 0;

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var withoutMarkup = HtmlRegex().Replace(value, " ");
        var decoded = WebUtility.HtmlDecode(withoutMarkup);
        return WhitespaceRegex().Replace(decoded, " ").Trim();
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlRegex();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("^CC (BY SA|BY) ([0-9]+\\.[0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex CreativeCommonsRegex();
}
