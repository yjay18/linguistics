using System.Globalization;
using Linguistics.Core.Content;

namespace Linguistics.AssetPipeline;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
            {
                PrintUsage();
                return args.Length == 0 ? 2 : 0;
            }

            var command = args[0];
            var options = CommandOptions.Parse(args[1..]);
            switch (command)
            {
                case "search":
                    await SearchAsync(options, cancellation.Token);
                    break;
                case "fetch":
                    await FetchAsync(options, cancellation.Token);
                    break;
                case "process-wikimedia":
                    ProcessWikimedia(options);
                    break;
                case "import-generated":
                    ImportGenerated(options);
                    break;
                case "audit":
                    Audit(options);
                    break;
                default:
                    throw new ArgumentException($"Unknown command '{command}'. Run with --help for usage.");
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Asset pipeline cancelled.");
            return 130;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static async Task SearchAsync(
        CommandOptions options,
        CancellationToken cancellationToken)
    {
        var query = options.Required("query");
        var limit = options.Integer("limit", 20);
        using var client = new WikimediaCommonsClient();
        var candidates = await client.SearchAsync(query, limit, cancellationToken);
        var output = options.Optional("output");
        if (output is null)
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                candidates,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            RefuseOverwrite(output, options.Flag("replace"));
            AssetManifestWriter.Write(output, candidates);
            Console.WriteLine($"Wrote {candidates.Count} allowed Commons candidate(s) to {output}.");
        }
    }

    private static async Task FetchAsync(
        CommandOptions options,
        CancellationToken cancellationToken)
    {
        var pageId = options.Integer("page-id");
        var sourcePath = options.Required("source");
        var recordPath = options.Required("record");
        var replace = options.Flag("replace");
        RefuseOverwrite(sourcePath, replace);
        RefuseOverwrite(recordPath, replace);

        using var client = new WikimediaCommonsClient();
        var candidate = await client.GetCandidateAsync(pageId, cancellationToken);
        var bytes = await client.DownloadAsync(candidate, cancellationToken);
        var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;
        Directory.CreateDirectory(sourceDirectory);
        await File.WriteAllBytesAsync(sourcePath, bytes, cancellationToken);
        var record = new FetchedWikimediaSource(
            candidate,
            DateOnly.FromDateTime(DateTime.UtcNow),
            WikimediaCommonsClient.Sha256(bytes),
            Path.GetFileName(sourcePath));
        AssetManifestWriter.Write(recordPath, record);
        Console.WriteLine(
            $"Fetched '{candidate.Title}' ({bytes.LongLength} bytes, {record.OriginalSha256}) with complete source metadata.");
    }

    private static void ProcessWikimedia(CommandOptions options)
    {
        var sourcePath = options.Required("source");
        var sourceRecordPath = options.Required("record");
        var manifestPath = options.Required("manifest");
        var outputPath = options.Required("output");
        var assetId = options.Required("asset-id");
        var packId = options.Required("pack-id");
        var packVersion = options.Integer("pack-version");
        var replace = options.Flag("replace");
        RefuseOverwrite(outputPath, replace);
        var source = AssetManifestWriter.Read<FetchedWikimediaSource>(sourceRecordPath);
        var processed = AssetImageProcessor.Process(sourcePath, outputPath, ProcessingOptions(options));
        var record = AssetManifestWriter.WikimediaRecord(
            assetId,
            manifestPath,
            outputPath,
            source,
            processed,
            options.Required("qa-notes"));
        AssetManifestWriter.Upsert(manifestPath, packId, packVersion, record, replace);
        Console.WriteLine(
            $"Processed {assetId}: {processed.Width}x{processed.Height}, {processed.ByteSize} bytes, {processed.Sha256}.");
    }

    private static void ImportGenerated(CommandOptions options)
    {
        var sourcePath = options.Required("source");
        var manifestPath = options.Required("manifest");
        var outputPath = options.Required("output");
        var assetId = options.Required("asset-id");
        var packId = options.Required("pack-id");
        var packVersion = options.Integer("pack-version");
        var replace = options.Flag("replace");
        RefuseOverwrite(outputPath, replace);
        var processed = AssetImageProcessor.Process(sourcePath, outputPath, ProcessingOptions(options));
        var record = AssetManifestWriter.GeneratedRecord(
            assetId,
            manifestPath,
            sourcePath,
            outputPath,
            options.Required("title"),
            options.Required("generator"),
            options.Required("prompt-summary"),
            processed,
            options.Required("qa-notes"));
        AssetManifestWriter.Upsert(manifestPath, packId, packVersion, record, replace);
        Console.WriteLine(
            $"Imported generated draft {assetId}: {processed.Width}x{processed.Height}, {processed.ByteSize} bytes, {processed.Sha256}.");
    }

    private static void Audit(CommandOptions options)
    {
        var contentRoot = options.Required("content-root");
        var catalog = ContentPackLoader.LoadDirectory(
            contentRoot,
            ContentLoadPolicy.AuthoringPreview);
        Console.WriteLine($"Validated {catalog.Assets.Count} asset(s) from {catalog.Packs.Count} pack(s).");
        foreach (var group in catalog.Assets
                     .GroupBy(asset => asset.PackId, StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Console.WriteLine(
                $"{group.Key}: {group.Count()} asset(s), {group.Sum(asset => asset.Record.ByteSize)} bytes.");
            foreach (var asset in group.OrderBy(asset => asset.Record.Id, StringComparer.Ordinal))
            {
                Console.WriteLine(
                    $"  {asset.Record.Id} | {asset.Record.Provenance} | {asset.Record.License.Identifier} | {asset.Record.ByteSize} bytes | {asset.Record.Sha256}");
            }
        }
    }

    private static ImageProcessingOptions ProcessingOptions(CommandOptions options) =>
        new(
            options.Integer("max-dimension", 900),
            options.Long("max-bytes", 300 * 1024),
            options.Optional("crop") is { } crop ? ParseCrop(crop) : null,
            options.Optional("background"),
            options.Integer("background-threshold", 28),
            options.Integer("background-feather", 18));

    private static ImageCrop ParseCrop(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || parts.Any(part => !int.TryParse(
                part,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out _)))
        {
            throw new ArgumentException("A crop uses x,y,width,height integers.");
        }

        return new ImageCrop(
            int.Parse(parts[0], CultureInfo.InvariantCulture),
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            int.Parse(parts[2], CultureInfo.InvariantCulture),
            int.Parse(parts[3], CultureInfo.InvariantCulture));
    }

    private static void RefuseOverwrite(string path, bool replace)
    {
        if (!replace && File.Exists(path))
        {
            throw new IOException($"'{path}' already exists. Pass --replace only after reviewing the replacement.");
        }
    }

    private static void PrintUsage() => Console.WriteLine(
        """
        Linguistics asset authoring pipeline. Network access exists only in search and fetch.

        search --query <keywords> [--limit 20] [--output candidates.json] [--replace]
        fetch --page-id <id> --source <original-file> --record <source.json> [--replace]
        process-wikimedia --source <original-file> --record <source.json> --manifest <assets.json>
          --pack-id <id> --pack-version <n> --asset-id <id> --output <pack/assets/file.png>
          --qa-notes <notes> [--max-dimension 900] [--max-bytes 307200]
          [--crop x,y,width,height] [--background #RRGGBB] [--replace]
        import-generated --source <original-file> --manifest <assets.json> --pack-id <id>
          --pack-version <n> --asset-id <id> --output <pack/assets/file.png> --title <title>
          --generator <name> --prompt-summary <summary> --qa-notes <notes>
          [--max-dimension 900] [--max-bytes 307200] [--crop ...] [--background ...] [--replace]
        audit --content-root <content-directory>
        """);

    private sealed class CommandOptions
    {
        private readonly Dictionary<string, string?> _values;

        private CommandOptions(Dictionary<string, string?> values) => _values = values;

        public static CommandOptions Parse(string[] args)
        {
            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            for (var index = 0; index < args.Length; index++)
            {
                var token = args[index];
                if (!token.StartsWith("--", StringComparison.Ordinal) || token.Length == 2)
                {
                    throw new ArgumentException($"Unexpected argument '{token}'. Options start with --.");
                }

                var name = token[2..];
                if (values.ContainsKey(name))
                {
                    throw new ArgumentException($"Option '--{name}' was supplied more than once.");
                }

                if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    values[name] = args[++index];
                }
                else
                {
                    values[name] = null;
                }
            }

            return new CommandOptions(values);
        }

        public string Required(string name) =>
            _values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException($"Missing required option '--{name}'.");

        public string? Optional(string name) =>
            _values.TryGetValue(name, out var value) ? value : null;

        public bool Flag(string name) =>
            _values.TryGetValue(name, out var value)
                ? value is null
                    ? true
                    : throw new ArgumentException($"Flag '--{name}' does not take a value.")
                : false;

        public int Integer(string name, int? fallback = null) =>
            _values.TryGetValue(name, out var value) &&
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback ?? throw new ArgumentException($"Option '--{name}' needs an integer value.");

        public long Long(string name, long fallback) =>
            _values.TryGetValue(name, out var value) &&
            long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;
    }
}
