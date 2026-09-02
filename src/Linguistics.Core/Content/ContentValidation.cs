using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Content;

public sealed record ContentValidationError(
    string Code,
    string PackId,
    string Path,
    string Message,
    string? LessonId = null,
    string? Parameter = null)
{
    public override string ToString()
    {
        var templateScope = LessonId is null
            ? string.Empty
            : $" [lesson={LessonId}, parameter={Parameter ?? "<none>"}]";
        return $"[{Code}] {PackId}/{Path}{templateScope}: {Message}";
    }
}

public sealed class ContentValidationException : Exception
{
    public ContentValidationException(IReadOnlyList<ContentValidationError> errors)
        : base(string.Join(Environment.NewLine, errors.Select(error => error.ToString())))
    {
        Errors = errors;
    }

    public IReadOnlyList<ContentValidationError> Errors { get; }
}

public static class ContentPackLoader
{
    private const int MaximumPackCount = 64;
    private const long MaximumPackBytes = 16_777_216;
    private const long MaximumAssetManifestBytes = 4_194_304;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        AllowTrailingCommas = false,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    public static ValidatedContentCatalog LoadDirectory(
        string rootDirectory,
        ContentLoadPolicy policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }

        if (!Directory.Exists(rootDirectory))
        {
            throw Error("directory.missing", "catalog", rootDirectory, "The content directory does not exist.");
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(rootDirectory, "pack.json", SearchOption.AllDirectories)
                .OrderBy(path => Path.GetRelativePath(rootDirectory, path), StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw Error("directory.read", "catalog", rootDirectory, exception.Message);
        }

        if (files.Length == 0)
        {
            throw Error("directory.empty", "catalog", rootDirectory, "No pack.json files were found.");
        }

        if (files.Length > MaximumPackCount)
        {
            throw Error(
                "directory.limit",
                "catalog",
                rootDirectory,
                $"The content directory contains {files.Length} packs; the limit is {MaximumPackCount}.");
        }

        var packs = new List<ContentPackDocument>(files.Length);
        var assetManifests = new List<ContentAssetManifestLocation>(files.Length);
        var decodeErrors = new List<ContentValidationError>();
        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(rootDirectory, file);
            var packDirectory = Path.GetDirectoryName(file)!;
            try
            {
                var length = new FileInfo(file).Length;
                if (length > MaximumPackBytes)
                {
                    decodeErrors.Add(new ContentValidationError(
                        "file.limit",
                        relativePath,
                        "$",
                        $"Pack size {length} bytes exceeds the {MaximumPackBytes}-byte limit."));
                    continue;
                }

                using var stream = File.OpenRead(file);
                var pack = JsonSerializer.Deserialize<ContentPackDocument>(stream, SerializerOptions);
                if (pack is null)
                {
                    decodeErrors.Add(new ContentValidationError(
                        "decode.null",
                        relativePath,
                        "$",
                        "The pack decoded to null."));
                    continue;
                }

                packs.Add(pack);
                if (pack.Manifest is null)
                {
                    continue;
                }

                var assetManifestPath = Path.Combine(packDirectory, "assets.json");
                try
                {
                    if (File.Exists(assetManifestPath))
                    {
                        var assetManifestLength = new FileInfo(assetManifestPath).Length;
                        if (assetManifestLength > MaximumAssetManifestBytes)
                        {
                            decodeErrors.Add(new ContentValidationError(
                                "asset.manifest.limit",
                                pack.Manifest.Id,
                                "assets.json",
                                $"Asset manifest size {assetManifestLength} bytes exceeds the {MaximumAssetManifestBytes}-byte limit."));
                            continue;
                        }

                        using var assetManifestStream = File.OpenRead(assetManifestPath);
                        var assetManifest = JsonSerializer.Deserialize<ContentAssetManifest>(
                            assetManifestStream,
                            SerializerOptions);
                        if (assetManifest is null)
                        {
                            decodeErrors.Add(new ContentValidationError(
                                "asset.manifest.null",
                                pack.Manifest.Id,
                                "assets.json",
                                "The asset manifest decoded to null."));
                            continue;
                        }

                        assetManifests.Add(new ContentAssetManifestLocation(
                            assetManifest,
                            packDirectory,
                            assetManifestPath));
                    }
                    else if (Directory.Exists(Path.Combine(packDirectory, "assets")) &&
                             Directory.EnumerateFiles(
                                     Path.Combine(packDirectory, "assets"),
                                     "*",
                                     SearchOption.AllDirectories)
                                 .Any(path => Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg"))
                    {
                        decodeErrors.Add(new ContentValidationError(
                            "asset.manifest.missing",
                            pack.Manifest.Id,
                            "assets.json",
                            "A pack containing bundled images needs an assets.json manifest."));
                    }
                }
                catch (JsonException exception)
                {
                    decodeErrors.Add(new ContentValidationError(
                        "asset.manifest.json",
                        pack.Manifest.Id,
                        exception.Path is null ? "assets.json" : $"assets.json:{exception.Path}",
                        exception.Message));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    decodeErrors.Add(new ContentValidationError(
                        "asset.manifest.read",
                        pack.Manifest.Id,
                        "assets.json",
                        exception.Message));
                }
            }
            catch (JsonException exception)
            {
                decodeErrors.Add(new ContentValidationError(
                    "decode.json",
                    relativePath,
                    exception.Path ?? "$",
                    exception.Message));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                decodeErrors.Add(new ContentValidationError(
                    "file.read",
                    relativePath,
                    "$",
                    exception.Message));
            }
        }

        if (decodeErrors.Count > 0)
        {
            throw new ContentValidationException(Order(decodeErrors));
        }

        var assetValidationErrors = ContentAssetValidator.Validate(
            packs,
            assetManifests,
            policy,
            out var validatedAssets);
        var validationErrors = ContentPackValidator.Validate(
            packs,
            policy,
            LessonTemplateSchemas.All,
            validatedAssets.Select(asset => asset.Record.Id).ToArray());
        validationErrors = validationErrors
            .Concat(assetValidationErrors)
            .OrderBy(error => error.PackId, StringComparer.Ordinal)
            .ThenBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)
            .ToArray();
        if (validationErrors.Count > 0)
        {
            throw new ContentValidationException(validationErrors);
        }

        return new ValidatedContentCatalog(
            packs.OrderBy(pack => pack.Manifest.Id, StringComparer.Ordinal).ToArray(),
            validatedAssets,
            policy);
    }

    private static ContentValidationException Error(
        string code,
        string packId,
        string path,
        string message) =>
        new([new ContentValidationError(code, packId, path, message)]);

    private static IReadOnlyList<ContentValidationError> Order(
        IEnumerable<ContentValidationError> errors) =>
        errors
            .OrderBy(error => error.PackId, StringComparer.Ordinal)
            .ThenBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)
            .ToArray();
}

public static class ContentPackValidator
{
    public const int SupportedSchemaVersion = 3;
    public const int SupportedPackVersion = 1;

    private static readonly HashSet<string> AllowedCefr = new(StringComparer.Ordinal)
    {
        "Pre-A1",
        "A1",
        "A2",
        "B1",
        "B2",
        "C1",
        "C2",
    };

    public static IReadOnlyList<ContentValidationError> Validate(
        IReadOnlyList<ContentPackDocument> packs,
        ContentLoadPolicy policy) =>
        Validate(packs, policy, LessonTemplateSchemas.All, []);

    public static IReadOnlyList<ContentValidationError> Validate(
        IReadOnlyList<ContentPackDocument> packs,
        ContentLoadPolicy policy,
        IReadOnlyList<LessonTemplateSchema> templateSchemas,
        IReadOnlyCollection<string> availableAssetIds)
    {
        ArgumentNullException.ThrowIfNull(packs);
        ArgumentNullException.ThrowIfNull(templateSchemas);
        ArgumentNullException.ThrowIfNull(availableAssetIds);
        if (!Enum.IsDefined(policy))
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }

        var errors = new List<ContentValidationError>();
        if (packs.Count == 0)
        {
            errors.Add(new ContentValidationError(
                "catalog.empty",
                "catalog",
                "$",
                "At least one content pack is required."));
            return Order(errors);
        }

        foreach (var (pack, index) in packs.Select((pack, index) => (pack, index)))
        {
            ValidatePackShape(pack, index, policy, errors);
        }

        var validPacks = packs.Where(pack => pack?.Manifest is not null).ToArray();
        ValidateGlobalIds(validPacks, errors);
        ValidateCourseCapacity(validPacks, errors);

        var packGroups = validPacks
            .Where(pack => IsCanonicalIdentifier(pack.Manifest.Id))
            .GroupBy(pack => pack.Manifest.Id, StringComparer.Ordinal)
            .ToArray();
        foreach (var duplicate in packGroups.Where(group => group.Count() > 1))
        {
            Add(errors, "id.duplicate", duplicate.Key, "manifest.id", $"Pack ID '{duplicate.Key}' appears more than once.");
        }

        var packsById = packGroups
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        ValidateDependencies(validPacks, packsById, errors);

        var concepts = UniqueItems(
            validPacks.SelectMany(pack => Items(pack.Concepts).Select(item => (item.Id, item, pack.Manifest.Id))),
            StringComparer.Ordinal);
        var tasks = UniqueItems(
            validPacks.SelectMany(pack => Items(pack.Tasks).Select(item => (item.Id, item, pack.Manifest.Id))),
            StringComparer.Ordinal);
        var errorRules = UniqueItems(
            validPacks.SelectMany(pack => Items(pack.ErrorRules).Select(item => (item.Id, item, pack.Manifest.Id))),
            StringComparer.Ordinal);
        var feedback = UniqueItems(
            validPacks.SelectMany(pack => Items(pack.FeedbackTemplates).Select(item => (item.Id, item, pack.Manifest.Id))),
            StringComparer.Ordinal);

        foreach (var pack in validPacks)
        {
            ValidatePackReferences(pack, concepts, tasks, errorRules, feedback, packsById, errors);
        }

        ValidateLessonTemplates(
            validPacks,
            templateSchemas,
            concepts.Keys.ToHashSet(StringComparer.Ordinal),
            UniqueStrings(validPacks.SelectMany(NamedExampleIds)),
            tasks.Keys.ToHashSet(StringComparer.Ordinal),
            availableAssetIds.ToHashSet(StringComparer.Ordinal),
            errors);
        ValidateConceptCycles(concepts, errors);
        return Order(errors);
    }

    private static void ValidateCourseCapacity(
        IReadOnlyList<ContentPackDocument> packs,
        ICollection<ContentValidationError> errors)
    {
        foreach (var language in packs
                     .Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage)
                     .SelectMany(pack => Items(pack.Concepts))
                     .Where(concept => concept is not null && !string.IsNullOrWhiteSpace(concept.Language))
                     .GroupBy(concept => concept.Language, StringComparer.Ordinal)
                     .Where(group => group.Count() > CourseCatalogConfiguration.MaximumLessonCount))
        {
            Add(
                errors,
                "catalog.limit",
                "catalog",
                $"languages.{language.Key}.concepts",
                $"Language '{language.Key}' contains {language.Count()} concepts; the limit is {CourseCatalogConfiguration.MaximumLessonCount}.");
        }
    }

    private static void ValidatePackShape(
        ContentPackDocument pack,
        int index,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        if (pack is null)
        {
            Add(errors, "pack.missing", $"pack-{index}", "$", "A pack document is missing.");
            return;
        }

        if (pack.Manifest is null)
        {
            Add(errors, "manifest.missing", $"pack-{index}", "manifest", "The pack manifest is missing.");
            return;
        }

        var packId = TextOrFallback(pack.Manifest.Id, $"pack-{index}");
        ValidateId(pack.Manifest.Id, packId, "manifest.id", errors);
        if (pack.Manifest.Version != SupportedPackVersion)
        {
            Add(
                errors,
                "version.unsupported",
                packId,
                "manifest.version",
                $"Pack version {pack.Manifest.Version} is unsupported; expected {SupportedPackVersion}.");
        }

        if (pack.Manifest.SchemaVersion != SupportedSchemaVersion)
        {
            Add(
                errors,
                "schema.unsupported",
                packId,
                "manifest.schemaVersion",
                $"Schema version {pack.Manifest.SchemaVersion} is unsupported; expected {SupportedSchemaVersion}.");
        }

        if (!Enum.IsDefined(pack.Manifest.Kind))
        {
            Add(errors, "pack.kind", packId, "manifest.kind", "The pack kind is invalid.");
        }

        ValidateLanguages(pack.Manifest.Languages, packId, "manifest.languages", errors);
        ValidateLanguages(
            pack.Manifest.InstructionLanguages,
            packId,
            "manifest.instructionLanguages",
            errors);
        ValidateDependenciesShape(pack.Manifest.Dependencies, packId, errors);
        ValidateLicense(pack.Manifest.License, packId, "manifest.license", policy, errors);
        ValidateReview(pack.Manifest.Review, packId, "manifest.review", policy, errors);

        if (pack.Sources is null)
        {
            Add(errors, "source.collection", packId, "sources", "The source collection is missing.");
        }
        else
        {
            foreach (var (source, sourceIndex) in pack.Sources.Select((source, index) => (source, index)))
            {
                ValidateSource(source, sourceIndex, packId, policy, errors);
            }
        }

        var instructionLanguages = Items(pack.Manifest.InstructionLanguages)
            .ToHashSet(StringComparer.Ordinal);

        ValidateCollections(pack, packId, errors);
        foreach (var (concept, itemIndex) in Items(pack.Concepts).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateConcept(concept, itemIndex, packId, policy, instructionLanguages, errors);
        }

        foreach (var (entry, itemIndex) in Items(pack.Lexicon).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateLexicalEntry(entry, itemIndex, packId, policy, instructionLanguages, errors);
        }

        foreach (var (task, itemIndex) in Items(pack.Tasks).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateTask(task, itemIndex, packId, policy, instructionLanguages, errors);
        }

        foreach (var (rule, itemIndex) in Items(pack.ErrorRules).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateErrorRule(rule, itemIndex, packId, policy, errors);
        }

        foreach (var (template, itemIndex) in Items(pack.FeedbackTemplates).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateFeedback(template, itemIndex, packId, policy, instructionLanguages, errors);
        }

        foreach (var (rubric, itemIndex) in Items(pack.Rubrics).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateRubric(rubric, itemIndex, packId, policy, errors);
        }

        foreach (var (utterance, itemIndex) in Items(pack.PronunciationUtterances).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidatePronunciation(utterance, itemIndex, packId, policy, errors);
        }

        foreach (var (mapping, itemIndex) in Items(pack.TransferMappings).Select((item, itemIndex) => (item, itemIndex)))
        {
            ValidateTransferMapping(mapping, itemIndex, packId, policy, instructionLanguages, errors);
        }

        ValidateLessons(pack, packId, errors);
        ValidateKindOwnership(pack, packId, errors);
    }

    private static void ValidateCollections(
        ContentPackDocument pack,
        string packId,
        ICollection<ContentValidationError> errors)
    {
        var collections = new (string Name, object? Value)[]
        {
            ("concepts", pack.Concepts),
            ("lexicon", pack.Lexicon),
            ("tasks", pack.Tasks),
            ("errorRules", pack.ErrorRules),
            ("feedbackTemplates", pack.FeedbackTemplates),
            ("rubrics", pack.Rubrics),
            ("pronunciationUtterances", pack.PronunciationUtterances),
            ("transferMappings", pack.TransferMappings),
        };
        foreach (var (name, value) in collections.Where(collection => collection.Value is null))
        {
            Add(errors, "collection.missing", packId, name, $"The '{name}' collection is missing.");
        }
    }

    private static void ValidateKindOwnership(
        ContentPackDocument pack,
        string packId,
        ICollection<ContentValidationError> errors)
    {
        if (pack.Manifest.Kind == ContentPackKind.TargetLanguage)
        {
            if (Items(pack.Concepts).Count == 0)
            {
                Add(errors, "pack.content", packId, "concepts", "A target-language pack needs concepts.");
            }

            if (Items(pack.TransferMappings).Count > 0)
            {
                Add(errors, "pack.ownership", packId, "transferMappings", "Transfer mappings must be in a transfer pack.");
            }
        }
        else if (pack.Manifest.Kind == ContentPackKind.Transfer)
        {
            if (Items(pack.TransferMappings).Count == 0)
            {
                Add(errors, "pack.content", packId, "transferMappings", "A transfer pack needs mappings.");
            }

            if (Items(pack.Concepts).Count > 0 ||
                Items(pack.Lexicon).Count > 0 ||
                Items(pack.Tasks).Count > 0 ||
                Items(pack.ErrorRules).Count > 0 ||
                Items(pack.FeedbackTemplates).Count > 0 ||
                Items(pack.Rubrics).Count > 0 ||
                Items(pack.PronunciationUtterances).Count > 0 ||
                Items(pack.Lessons).Count > 0)
            {
                Add(
                    errors,
                    "pack.ownership",
                    packId,
                    "$",
                    "A transfer pack may not duplicate target-language content.");
            }
        }
    }

    private static void ValidateLessons(
        ContentPackDocument pack,
        string packId,
        ICollection<ContentValidationError> errors)
    {
        if (pack.Lessons is null)
        {
            Add(errors, "lesson.collection", packId, "lessons", "The lesson collection is missing.");
            return;
        }

        var knownLessonIds = Items(pack.Concepts)
            .Where(concept => concept is not null && !string.IsNullOrWhiteSpace(concept.Id))
            .Select(concept => $"lesson.{concept.Id}")
            .ToHashSet(StringComparer.Ordinal);
        foreach (var (lesson, lessonIndex) in pack.Lessons.Select((lesson, index) => (lesson, index)))
        {
            var lessonPath = $"lessons[{lessonIndex}]";
            if (lesson is null)
            {
                Add(errors, "lesson.missing", packId, lessonPath, "A lesson binding is missing.");
                continue;
            }

            ValidateId(lesson.Id, packId, $"{lessonPath}.id", errors);
            if (!knownLessonIds.Contains(lesson.Id))
            {
                Add(
                    errors,
                    "lesson.reference",
                    packId,
                    $"{lessonPath}.id",
                    $"Lesson '{lesson.Id}' does not bind a concept projected by this pack.");
            }

            if (lesson.TemplateInstances is null || lesson.TemplateInstances.Count == 0)
            {
                Add(
                    errors,
                    "template.collection",
                    packId,
                    $"{lessonPath}.templateInstances",
                    "An authored lesson needs at least one ordered template instance.");
                continue;
            }

            foreach (var (instance, instanceIndex) in lesson.TemplateInstances.Select((instance, index) => (instance, index)))
            {
                var instancePath = $"{lessonPath}.templateInstances[{instanceIndex}]";
                if (instance is null)
                {
                    Add(
                        errors,
                        "template.instance",
                        packId,
                        instancePath,
                        "A template instance is missing.");
                    continue;
                }

                ValidateId(instance.Id, packId, $"{instancePath}.id", errors);
                ValidateId(instance.TemplateId.Value, packId, $"{instancePath}.templateId", errors);
                if (instance.TemplateVersion < 1)
                {
                    Add(
                        errors,
                        "template.version",
                        packId,
                        $"{instancePath}.templateVersion",
                        "A template version must be at least 1.");
                }

                if (instance.Parameters is null)
                {
                    Add(
                        errors,
                        "template.parameters",
                        packId,
                        $"{instancePath}.parameters",
                        "The template parameter object is missing.");
                }
            }
        }
    }

    private static void ValidateLessonTemplates(
        IReadOnlyList<ContentPackDocument> packs,
        IReadOnlyList<LessonTemplateSchema> templateSchemas,
        IReadOnlySet<string> conceptIds,
        IReadOnlySet<string> exampleIds,
        IReadOnlySet<string> taskIds,
        IReadOnlySet<string> assetIds,
        ICollection<ContentValidationError> errors)
    {
        var schemasById = templateSchemas
            .Where(schema => schema is not null)
            .GroupBy(schema => schema.Id)
            .ToDictionary(group => group.Key, group => group.ToArray());

        foreach (var pack in packs)
        {
            var instructionLanguages = RequiredInstructionLanguages(pack);
            foreach (var (lesson, lessonIndex) in Items(pack.Lessons).Select((lesson, index) => (lesson, index)))
            {
                if (lesson is null)
                {
                    continue;
                }

                foreach (var (instance, instanceIndex) in Items(lesson.TemplateInstances).Select((instance, index) => (instance, index)))
                {
                    if (instance is null)
                    {
                        continue;
                    }

                    var instancePath = $"lessons[{lessonIndex}].templateInstances[{instanceIndex}]";
                    if (!schemasById.TryGetValue(instance.TemplateId, out var schemas))
                    {
                        AddTemplate(
                            errors,
                            "template.unknown",
                            pack.Manifest.Id,
                            $"{instancePath}.templateId",
                            lesson.Id,
                            "templateId",
                            $"Template '{instance.TemplateId}' is not registered.");
                        continue;
                    }

                    var matchingSchemas = schemas
                        .Where(schema => schema.Version == instance.TemplateVersion)
                        .ToArray();
                    if (matchingSchemas.Length == 0)
                    {
                        AddTemplate(
                            errors,
                            "template.version",
                            pack.Manifest.Id,
                            $"{instancePath}.templateVersion",
                            lesson.Id,
                            "templateVersion",
                            $"Template '{instance.TemplateId}' does not support version {instance.TemplateVersion}.");
                        continue;
                    }

                    if (matchingSchemas.Length > 1)
                    {
                        AddTemplate(
                            errors,
                            "template.schema",
                            pack.Manifest.Id,
                            $"{instancePath}.templateId",
                            lesson.Id,
                            "templateId",
                            $"Template '{instance.TemplateId}' version {instance.TemplateVersion} is registered more than once.");
                        continue;
                    }

                    ValidateTemplateParameters(
                        pack.Manifest.Id,
                        lesson.Id,
                        instancePath,
                        instance,
                        matchingSchemas[0],
                        instructionLanguages,
                        conceptIds,
                        exampleIds,
                        taskIds,
                        assetIds,
                        errors);
                }
            }
        }
    }

    private static void ValidateTemplateParameters(
        string packId,
        string lessonId,
        string instancePath,
        TemplateInstance instance,
        LessonTemplateSchema schema,
        IReadOnlySet<string> instructionLanguages,
        IReadOnlySet<string> conceptIds,
        IReadOnlySet<string> exampleIds,
        IReadOnlySet<string> taskIds,
        IReadOnlySet<string> assetIds,
        ICollection<ContentValidationError> errors)
    {
        if (schema.Version < 1 || schema.Parameters is null)
        {
            AddTemplate(
                errors,
                "template.schema",
                packId,
                $"{instancePath}.templateId",
                lessonId,
                "templateId",
                $"Template '{schema.Id}' has an invalid registered schema.");
            return;
        }

        var definitions = schema.Parameters
            .Where(definition => definition is not null)
            .GroupBy(definition => definition.Name, StringComparer.Ordinal)
            .ToArray();
        foreach (var invalid in definitions.Where(group =>
                     group.Count() != 1 ||
                     !IsCanonicalIdentifier(group.Key) ||
                     !Enum.IsDefined(group.First().Kind)))
        {
            AddTemplate(
                errors,
                "template.schema",
                packId,
                $"{instancePath}.templateId",
                lessonId,
                invalid.Key,
                $"Template '{schema.Id}' has an invalid or duplicate parameter definition '{invalid.Key}'.");
        }

        var definitionsByName = definitions
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        if (instance.Parameters is null)
        {
            return;
        }

        foreach (var required in definitionsByName.Values.Where(definition =>
                     definition.IsRequired && !instance.Parameters.ContainsKey(definition.Name)))
        {
            AddTemplate(
                errors,
                "template.parameter.missing",
                packId,
                $"{instancePath}.parameters.{required.Name}",
                lessonId,
                required.Name,
                $"Required parameter '{required.Name}' is missing.");
        }

        foreach (var (parameterName, value) in instance.Parameters)
        {
            var parameterPath = $"{instancePath}.parameters.{parameterName}";
            if (!definitionsByName.TryGetValue(parameterName, out var definition))
            {
                AddTemplate(
                    errors,
                    "template.parameter.unknown",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Parameter '{parameterName}' is not defined by template '{schema.Id}'.");
                continue;
            }

            if (value is null || value.Kind != definition.Kind)
            {
                AddTemplate(
                    errors,
                    "template.parameter.type",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Parameter '{parameterName}' must be '{definition.Kind}'.");
                continue;
            }

            ValidateTemplateParameterValue(
                packId,
                lessonId,
                parameterName,
                parameterPath,
                value,
                instructionLanguages,
                conceptIds,
                exampleIds,
                taskIds,
                assetIds,
                errors);
        }
    }

    private static void ValidateTemplateParameterValue(
        string packId,
        string lessonId,
        string parameterName,
        string parameterPath,
        TemplateParameterValue value,
        IReadOnlySet<string> instructionLanguages,
        IReadOnlySet<string> conceptIds,
        IReadOnlySet<string> exampleIds,
        IReadOnlySet<string> taskIds,
        IReadOnlySet<string> assetIds,
        ICollection<ContentValidationError> errors)
    {
        switch (value.Kind)
        {
            case TemplateParameterKind.Text:
                ValidateScalarShape(value, packId, lessonId, parameterName, parameterPath, errors);
                break;
            case TemplateParameterKind.TextByLanguage:
                ValidateTextMap(
                    value,
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    instructionLanguages,
                    errors);
                break;
            case TemplateParameterKind.ConceptReference:
                ValidateTemplateReference(
                    value,
                    conceptIds,
                    "concept",
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    errors);
                break;
            case TemplateParameterKind.ExampleReference:
                ValidateTemplateReference(
                    value,
                    exampleIds,
                    "example",
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    errors);
                break;
            case TemplateParameterKind.AssetReference:
                ValidateTemplateReference(
                    value,
                    assetIds,
                    "asset",
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    errors);
                break;
            case TemplateParameterKind.TaskReference:
                ValidateTemplateReference(
                    value,
                    taskIds,
                    "task",
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    errors);
                break;
            case TemplateParameterKind.OptionList:
                ValidateOptions(
                    value,
                    assetIds,
                    packId,
                    lessonId,
                    parameterName,
                    parameterPath,
                    errors);
                break;
            default:
                AddTemplate(
                    errors,
                    "template.parameter.type",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Parameter '{parameterName}' has an invalid kind.");
                break;
        }
    }

    private static void ValidateScalarShape(
        TemplateParameterValue value,
        string packId,
        string lessonId,
        string parameterName,
        string parameterPath,
        ICollection<ContentValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value.Value) || value.TextByLanguage is not null || value.Options is not null)
        {
            AddTemplate(
                errors,
                "template.parameter.type",
                packId,
                parameterPath,
                lessonId,
                parameterName,
                $"Parameter '{parameterName}' needs one non-empty scalar value.");
        }
    }

    private static void ValidateTextMap(
        TemplateParameterValue value,
        string packId,
        string lessonId,
        string parameterName,
        string parameterPath,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        if (value.Value is not null || value.Options is not null || value.TextByLanguage is null || value.TextByLanguage.Count == 0)
        {
            AddTemplate(
                errors,
                "template.parameter.type",
                packId,
                parameterPath,
                lessonId,
                parameterName,
                $"Parameter '{parameterName}' needs a non-empty language-to-text map.");
            return;
        }

        foreach (var (language, text) in value.TextByLanguage)
        {
            if (!IsCanonicalLanguage(language) || string.IsNullOrWhiteSpace(text))
            {
                AddTemplate(
                    errors,
                    "template.parameter.language",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Parameter '{parameterName}' has an invalid language or empty text for '{language}'.");
            }
        }

        foreach (var language in instructionLanguages.Where(language =>
                     !value.TextByLanguage.TryGetValue(language, out var text) || string.IsNullOrWhiteSpace(text)))
        {
            AddTemplate(
                errors,
                "template.parameter.language",
                packId,
                parameterPath,
                lessonId,
                parameterName,
                $"Parameter '{parameterName}' is missing instruction-language text for '{language}'.");
        }
    }

    private static void ValidateTemplateReference(
        TemplateParameterValue value,
        IReadOnlySet<string> knownIds,
        string kind,
        string packId,
        string lessonId,
        string parameterName,
        string parameterPath,
        ICollection<ContentValidationError> errors)
    {
        if (value.TextByLanguage is not null || value.Options is not null ||
            string.IsNullOrWhiteSpace(value.Value) || !knownIds.Contains(value.Value))
        {
            AddTemplate(
                errors,
                $"template.reference.{kind}",
                packId,
                parameterPath,
                lessonId,
                parameterName,
                $"Referenced {kind} '{value.Value}' does not resolve.");
        }
    }

    private static void ValidateOptions(
        TemplateParameterValue value,
        IReadOnlySet<string> assetIds,
        string packId,
        string lessonId,
        string parameterName,
        string parameterPath,
        ICollection<ContentValidationError> errors)
    {
        if (value.Value is not null || value.TextByLanguage is not null || value.Options is null || value.Options.Count == 0)
        {
            AddTemplate(
                errors,
                "template.parameter.type",
                packId,
                parameterPath,
                lessonId,
                parameterName,
                $"Parameter '{parameterName}' needs a non-empty option list.");
            return;
        }

        var optionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in value.Options)
        {
            if (option is null || !IsCanonicalIdentifier(option.Id) ||
                !optionIds.Add(option.Id) || string.IsNullOrWhiteSpace(option.Label))
            {
                AddTemplate(
                    errors,
                    "template.option",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Parameter '{parameterName}' contains a missing, duplicate, or invalid option.");
                continue;
            }

            if (option.AssetReferenceId is { } assetId && !assetIds.Contains(assetId))
            {
                AddTemplate(
                    errors,
                    "template.reference.asset",
                    packId,
                    parameterPath,
                    lessonId,
                    parameterName,
                    $"Referenced asset '{assetId}' does not resolve.");
            }
        }
    }

    private static IReadOnlySet<string> RequiredInstructionLanguages(ContentPackDocument pack) =>
        Items(pack.Manifest.InstructionLanguages).ToHashSet(StringComparer.Ordinal);

    private static bool IsCanonicalLanguage(string language)
    {
        try
        {
            return new LanguageCode(language).Value == language;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void ValidateSource(
        SourceRecord source,
        int index,
        string packId,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        var path = $"sources[{index}]";
        if (source is null)
        {
            Add(errors, "source.missing", packId, path, "A source record is missing.");
            return;
        }

        ValidateId(source.Id, packId, $"{path}.id", errors);
        RequireText(source.Title, "source.field", packId, $"{path}.title", "A source title is required.", errors);
        RequireText(source.Citation, "source.field", packId, $"{path}.citation", "A stable citation is required.", errors);
        RequireText(source.Claim, "source.field", packId, $"{path}.claim", "The supported claim must be named.", errors);
        RequireText(source.Notes, "source.field", packId, $"{path}.notes", "Source-use notes are required.", errors);
        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            Add(errors, "source.url", packId, $"{path}.url", "The source URL must be an absolute HTTP or HTTPS URL.");
        }

        ValidateLicense(source.License, packId, $"{path}.license", policy, errors);
    }

    private static void ValidateConcept(
        TargetConceptContent concept,
        int index,
        string packId,
        ContentLoadPolicy policy,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        var path = $"concepts[{index}]";
        if (concept is null)
        {
            Add(errors, "concept.missing", packId, path, "A concept is missing.");
            return;
        }

        ValidateId(concept.Id, packId, $"{path}.id", errors);
        ValidateLanguage(concept.Language, packId, $"{path}.language", errors);
        if (!Enum.IsDefined(concept.Type))
        {
            Add(errors, "concept.type", packId, $"{path}.type", "The concept type is invalid.");
        }

        ValidateCefr(concept.CefrApproximation, packId, $"{path}.cefrApproximation", errors);
        ValidateInstructionText(concept.Title, packId, $"{path}.title", instructionLanguages, errors);
        ValidateInstructionText(concept.Description, packId, $"{path}.description", instructionLanguages, errors);
        ValidateIdList(concept.PrerequisiteIds, packId, $"{path}.prerequisiteIds", errors);
        ValidateIdList(concept.ErrorRuleIds, packId, $"{path}.errorRuleIds", errors);
        ValidateTextList(concept.TaskTags, packId, $"{path}.taskTags", allowEmpty: false, errors);
        ValidateExamples(
            concept.Examples,
            packId,
            $"{path}.examples",
            allowEmpty: false,
            instructionLanguages,
            errors);
        ValidateExamples(
            concept.Counterexamples,
            packId,
            $"{path}.counterexamples",
            allowEmpty: true,
            instructionLanguages,
            errors);
        ValidateSourceIds(concept.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(concept.Review, packId, $"{path}.review", policy, errors);

        if (concept.SuccessCriteria is null)
        {
            Add(errors, "concept.success", packId, $"{path}.successCriteria", "Success criteria are required.");
        }
        else
        {
            if (concept.SuccessCriteria.MinimumAttempts is < 1 or > 100)
            {
                Add(errors, "concept.success", packId, $"{path}.successCriteria.minimumAttempts", "Minimum attempts must be between 1 and 100.");
            }

            if (!IsUnitInterval(concept.SuccessCriteria.MinimumAccuracy))
            {
                Add(errors, "concept.success", packId, $"{path}.successCriteria.minimumAccuracy", "Minimum accuracy must be between 0 and 1.");
            }

            ValidateIdList(
                concept.SuccessCriteria.RequiredEvaluatorIds,
                packId,
                $"{path}.successCriteria.requiredEvaluatorIds",
                errors);
        }
    }

    private static void ValidateLexicalEntry(
        LexicalEntryContent entry,
        int index,
        string packId,
        ContentLoadPolicy policy,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        var path = $"lexicon[{index}]";
        if (entry is null)
        {
            Add(errors, "lexicon.missing", packId, path, "A lexical entry is missing.");
            return;
        }

        ValidateId(entry.Id, packId, $"{path}.id", errors);
        ValidateLanguage(entry.Language, packId, $"{path}.language", errors);
        RequireText(entry.Lemma, "lexicon.text", packId, $"{path}.lemma", "A lemma is required.", errors);
        ValidateInstructionText(entry.Meaning, packId, $"{path}.meaning", instructionLanguages, errors);
        if (entry.Article is { } article && string.IsNullOrWhiteSpace(article))
        {
            Add(errors, "lexicon.text", packId, $"{path}.article", "An article must be null or non-empty.");
        }

        ValidateIdList(entry.ConceptIds, packId, $"{path}.conceptIds", errors, allowEmpty: false);
        ValidateExamples(
            entry.Examples,
            packId,
            $"{path}.examples",
            allowEmpty: false,
            instructionLanguages,
            errors);
        ValidateSourceIds(entry.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(entry.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidateTask(
        TaskTemplateContent task,
        int index,
        string packId,
        ContentLoadPolicy policy,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        var path = $"tasks[{index}]";
        if (task is null)
        {
            Add(errors, "task.missing", packId, path, "A task template is missing.");
            return;
        }

        ValidateId(task.Id, packId, $"{path}.id", errors);
        ValidateLanguage(task.Language, packId, $"{path}.language", errors);
        ValidateCefr(task.CefrApproximation, packId, $"{path}.cefrApproximation", errors);
        RequireText(task.Domain, "task.text", packId, $"{path}.domain", "A task domain is required.", errors);
        ValidateInstructionText(task.Goal, packId, $"{path}.goal", instructionLanguages, errors);
        ValidateInstructionText(task.Context, packId, $"{path}.context", instructionLanguages, errors);
        ValidateInstructionText(task.LearnerRole, packId, $"{path}.learnerRole", instructionLanguages, errors);
        ValidateInstructionText(task.NpcRole, packId, $"{path}.npcRole", instructionLanguages, errors);
        ValidateIdList(task.RequiredFunctionIds, packId, $"{path}.requiredFunctionIds", errors, allowEmpty: false);
        ValidateIdList(task.EligibleConceptIds, packId, $"{path}.eligibleConceptIds", errors, allowEmpty: false);
        ValidateSourceIds(task.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(task.Review, packId, $"{path}.review", policy, errors);

        var states = Items(task.States);
        var evaluators = Items(task.Evaluators);
        var transitions = Items(task.Transitions);
        var success = Items(task.SuccessConditions);
        if (states.Count < 2)
        {
            Add(errors, "task.state", packId, $"{path}.states", "A task needs at least two states.");
        }

        var stateIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (state, stateIndex) in states.Select((state, stateIndex) => (state, stateIndex)))
        {
            var statePath = $"{path}.states[{stateIndex}]";
            if (state is null)
            {
                Add(errors, "task.state", packId, statePath, "A task state is missing.");
                continue;
            }

            ValidateId(state.Id, packId, $"{statePath}.id", errors);
            if (!stateIds.Add(state.Id))
            {
                Add(errors, "id.duplicate", packId, $"{statePath}.id", $"Task state ID '{state.Id}' appears more than once.");
            }

            ValidateTextList(state.AllowedIntents, packId, $"{statePath}.allowedIntents", allowEmpty: false, errors);
            ValidateTextList(state.ScriptedFallback, packId, $"{statePath}.scriptedFallback", allowEmpty: false, errors);
        }

        if (!stateIds.Contains(task.InitialStateId))
        {
            Add(errors, "task.state", packId, $"{path}.initialStateId", $"Initial state '{task.InitialStateId}' does not resolve.");
        }

        ValidateIdList(task.SuccessStateIds, packId, $"{path}.successStateIds", errors, allowEmpty: false);
        foreach (var stateId in Items(task.SuccessStateIds).Where(stateId => !stateIds.Contains(stateId)))
        {
            Add(errors, "task.state", packId, $"{path}.successStateIds", $"Success state '{stateId}' does not resolve.");
        }

        var evaluatorById = new Dictionary<string, TaskEvaluatorContent>(StringComparer.Ordinal);
        foreach (var (evaluator, evaluatorIndex) in evaluators.Select((evaluator, evaluatorIndex) => (evaluator, evaluatorIndex)))
        {
            var evaluatorPath = $"{path}.evaluators[{evaluatorIndex}]";
            if (evaluator is null)
            {
                Add(errors, "evaluator.missing", packId, evaluatorPath, "A task evaluator is missing.");
                continue;
            }

            ValidateId(evaluator.Id, packId, $"{evaluatorPath}.id", errors);
            if (!evaluatorById.TryAdd(evaluator.Id, evaluator))
            {
                Add(errors, "id.duplicate", packId, $"{evaluatorPath}.id", $"Evaluator ID '{evaluator.Id}' appears more than once in the task.");
            }

            if (!Enum.IsDefined(evaluator.Kind))
            {
                Add(errors, "evaluator.kind", packId, $"{evaluatorPath}.kind", "The evaluator kind is invalid.");
            }

            ValidateTextList(evaluator.ExpectedValues, packId, $"{evaluatorPath}.expectedValues", allowEmpty: false, errors);
            if (evaluator.Kind == TaskEvaluatorKind.RequiredToken && Items(evaluator.ExpectedValues).Count != 1)
            {
                Add(errors, "evaluator.pattern", packId, evaluatorPath, "A required-token evaluator needs exactly one value.");
            }

            if (evaluator.Kind == TaskEvaluatorKind.RequiredTokenSequence && Items(evaluator.ExpectedValues).Count < 2)
            {
                Add(errors, "evaluator.pattern", packId, evaluatorPath, "A token-sequence evaluator needs at least two values.");
            }
        }

        var usedEvaluators = new HashSet<string>(StringComparer.Ordinal);
        var transitionKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (transition, transitionIndex) in transitions.Select((transition, transitionIndex) => (transition, transitionIndex)))
        {
            var transitionPath = $"{path}.transitions[{transitionIndex}]";
            if (transition is null)
            {
                Add(errors, "task.transition", packId, transitionPath, "A task transition is missing.");
                continue;
            }

            if (!stateIds.Contains(transition.FromStateId) || !stateIds.Contains(transition.ToStateId))
            {
                Add(
                    errors,
                    "task.transition",
                    packId,
                    transitionPath,
                    $"Transition '{transition.FromStateId}' -> '{transition.ToStateId}' references an unknown state.");
            }

            if (!evaluatorById.TryGetValue(transition.EvaluatorId, out var transitionEvaluator))
            {
                Add(errors, "reference.broken", packId, $"{transitionPath}.evaluatorId", $"Evaluator '{transition.EvaluatorId}' does not resolve.");
            }
            else if (transitionEvaluator.Kind == TaskEvaluatorKind.StateReached)
            {
                Add(errors, "evaluator.coverage", packId, $"{transitionPath}.evaluatorId", "A state-reached evaluator cannot trigger a transition.");
            }

            usedEvaluators.Add(transition.EvaluatorId);
            var key = $"{transition.FromStateId}\0{transition.ToStateId}\0{transition.EvaluatorId}";
            if (!transitionKeys.Add(key))
            {
                Add(errors, "task.transition", packId, transitionPath, "The task repeats a transition.");
            }
        }

        foreach (var (condition, conditionIndex) in success.Select((condition, conditionIndex) => (condition, conditionIndex)))
        {
            var conditionPath = $"{path}.successConditions[{conditionIndex}]";
            if (condition is null)
            {
                Add(errors, "task.success", packId, conditionPath, "A success condition is missing.");
                continue;
            }

            ValidateInstructionText(
                condition.Description,
                packId,
                $"{conditionPath}.description",
                instructionLanguages,
                errors);
            if (!evaluatorById.TryGetValue(condition.EvaluatorId, out var evaluator))
            {
                Add(errors, "evaluator.coverage", packId, $"{conditionPath}.evaluatorId", $"Success evaluator '{condition.EvaluatorId}' does not resolve.");
            }
            else if (evaluator.Kind == TaskEvaluatorKind.StateReached &&
                     evaluator.ExpectedValues.Any(value => !stateIds.Contains(value)))
            {
                Add(errors, "task.state", packId, $"{conditionPath}.evaluatorId", "A state-reached evaluator references an unknown state.");
            }

            usedEvaluators.Add(condition.EvaluatorId);
        }

        foreach (var unused in evaluatorById.Keys.Where(id => !usedEvaluators.Contains(id)))
        {
            Add(errors, "evaluator.coverage", packId, $"{path}.evaluators", $"Evaluator '{unused}' is not used by a transition or success condition.");
        }

        ValidateTaskReachability(task, states, transitions, stateIds, packId, path, errors);
    }

    private static void ValidateTaskReachability(
        TaskTemplateContent task,
        IReadOnlyList<TaskStateContent> states,
        IReadOnlyList<TaskTransitionContent> transitions,
        IReadOnlySet<string> stateIds,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (!stateIds.Contains(task.InitialStateId))
        {
            return;
        }

        var reachable = new HashSet<string>(StringComparer.Ordinal) { task.InitialStateId };
        var queue = new Queue<string>();
        queue.Enqueue(task.InitialStateId);
        while (queue.TryDequeue(out var current))
        {
            foreach (var next in transitions
                         .Where(transition => transition is not null && transition.FromStateId == current)
                         .Select(transition => transition.ToStateId)
                         .Where(stateIds.Contains))
            {
                if (reachable.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        foreach (var successState in Items(task.SuccessStateIds).Where(state => !reachable.Contains(state)))
        {
            Add(errors, "task.transition", packId, $"{path}.successStateIds", $"Success state '{successState}' is unreachable from '{task.InitialStateId}'.");
        }

        foreach (var unreachable in states.Where(state => state is not null && !reachable.Contains(state.Id)))
        {
            Add(errors, "task.transition", packId, $"{path}.states", $"State '{unreachable.Id}' is unreachable from '{task.InitialStateId}'.");
        }
    }

    private static void ValidateErrorRule(
        ErrorRuleContent rule,
        int index,
        string packId,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        var path = $"errorRules[{index}]";
        if (rule is null)
        {
            Add(errors, "error.missing", packId, path, "An error rule is missing.");
            return;
        }

        ValidateId(rule.Id, packId, $"{path}.id", errors);
        ValidateId(rule.TargetConceptId, packId, $"{path}.targetConceptId", errors);
        ValidateId(rule.FeedbackTemplateId, packId, $"{path}.feedbackTemplateId", errors);
        RequireText(rule.ExpectedProperty, "error.pattern", packId, $"{path}.expectedProperty", "The expected property is required.", errors);
        if (!Enum.IsDefined(rule.Severity))
        {
            Add(errors, "error.severity", packId, $"{path}.severity", "The error severity is invalid.");
        }

        if (rule.Pattern is null)
        {
            Add(errors, "error.pattern", packId, $"{path}.pattern", "An error pattern is required.");
        }
        else
        {
            if (!Enum.IsDefined(rule.Pattern.Kind))
            {
                Add(errors, "error.pattern", packId, $"{path}.pattern.kind", "The error pattern kind is invalid.");
            }

            ValidateTextList(rule.Pattern.Values, packId, $"{path}.pattern.values", allowEmpty: false, errors);
            var count = Items(rule.Pattern.Values).Count;
            if (rule.Pattern.Kind is ErrorPatternKind.RequiredToken or ErrorPatternKind.ForbiddenToken && count != 1)
            {
                Add(errors, "error.pattern", packId, $"{path}.pattern.values", "A single-token pattern needs exactly one value.");
            }

            if (rule.Pattern.Kind == ErrorPatternKind.RequiredTokenSequence && count < 2)
            {
                Add(errors, "error.pattern", packId, $"{path}.pattern.values", "A token-sequence pattern needs at least two values.");
            }
        }

        ValidateTextList(rule.Examples, packId, $"{path}.examples", allowEmpty: false, errors);
        ValidateTextList(rule.Counterexamples, packId, $"{path}.counterexamples", allowEmpty: false, errors);
        ValidateSourceIds(rule.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(rule.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidateFeedback(
        FeedbackTemplateContent template,
        int index,
        string packId,
        ContentLoadPolicy policy,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        var path = $"feedbackTemplates[{index}]";
        if (template is null)
        {
            Add(errors, "feedback.missing", packId, path, "A feedback template is missing.");
            return;
        }

        ValidateId(template.Id, packId, $"{path}.id", errors);
        ValidateLanguage(template.Language, packId, $"{path}.language", errors);
        ValidateInstructionText(template.Message, packId, $"{path}.message", instructionLanguages, errors);
        ValidateInstructionText(template.RetryPrompt, packId, $"{path}.retryPrompt", instructionLanguages, errors);
        ValidateSourceIds(template.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(template.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidateRubric(
        RubricContent rubric,
        int index,
        string packId,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        var path = $"rubrics[{index}]";
        if (rubric is null)
        {
            Add(errors, "rubric.missing", packId, path, "A rubric is missing.");
            return;
        }

        ValidateId(rubric.Id, packId, $"{path}.id", errors);
        ValidateId(rubric.TaskId, packId, $"{path}.taskId", errors);
        var dimensions = Items(rubric.Dimensions);
        if (dimensions.Count == 0)
        {
            Add(errors, "rubric.dimension", packId, $"{path}.dimensions", "A rubric needs dimensions.");
        }

        foreach (var duplicate in dimensions
                     .Where(dimension => dimension is not null)
                     .GroupBy(dimension => dimension.Dimension)
                     .Where(group => group.Count() > 1))
        {
            Add(errors, "rubric.dimension", packId, $"{path}.dimensions", $"Dimension '{duplicate.Key}' appears more than once.");
        }

        if (!dimensions.Any(dimension => dimension is not null && dimension.Dimension == TaskOutcomeDimension.CommunicativeSuccess))
        {
            Add(errors, "rubric.dimension", packId, $"{path}.dimensions", "A rubric must keep communicative success separate.");
        }

        foreach (var (dimension, dimensionIndex) in dimensions.Select((dimension, dimensionIndex) => (dimension, dimensionIndex)))
        {
            if (dimension is null)
            {
                Add(errors, "rubric.dimension", packId, $"{path}.dimensions[{dimensionIndex}]", "A rubric dimension is missing.");
                continue;
            }

            if (!Enum.IsDefined(dimension.Dimension))
            {
                Add(errors, "rubric.dimension", packId, $"{path}.dimensions[{dimensionIndex}].dimension", "The outcome dimension is invalid.");
            }

            ValidateId(dimension.EvaluatorId, packId, $"{path}.dimensions[{dimensionIndex}].evaluatorId", errors);
        }

        ValidateSourceIds(rubric.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(rubric.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidatePronunciation(
        PronunciationUtteranceContent utterance,
        int index,
        string packId,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        var path = $"pronunciationUtterances[{index}]";
        if (utterance is null)
        {
            Add(errors, "pronunciation.missing", packId, path, "A pronunciation utterance is missing.");
            return;
        }

        ValidateId(utterance.Id, packId, $"{path}.id", errors);
        ValidateLanguage(utterance.Language, packId, $"{path}.language", errors);
        ValidateLanguage(utterance.Locale, packId, $"{path}.locale", errors);
        RequireText(utterance.Text, "pronunciation.text", packId, $"{path}.text", "Utterance text is required.", errors);
        if (!Enum.IsDefined(utterance.Purpose))
        {
            Add(errors, "pronunciation.purpose", packId, $"{path}.purpose", "The pronunciation purpose is invalid.");
        }

        if (utterance.AssessmentMode != PronunciationAssessmentMode.None)
        {
            Add(errors, "pronunciation.assessment", packId, $"{path}.assessmentMode", "Milestone 3 utterances cannot claim pronunciation assessment.");
        }

        ValidateIdList(utterance.ConceptIds, packId, $"{path}.conceptIds", errors, allowEmpty: false);
        ValidateSourceIds(utterance.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(utterance.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidateTransferMapping(
        TransferMappingContent mapping,
        int index,
        string packId,
        ContentLoadPolicy policy,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        var path = $"transferMappings[{index}]";
        if (mapping is null)
        {
            Add(errors, "mapping.missing", packId, path, "A transfer mapping is missing.");
            return;
        }

        ValidateId(mapping.Id, packId, $"{path}.id", errors);
        ValidateLanguage(mapping.SourceLanguage, packId, $"{path}.sourceLanguage", errors);
        ValidateLanguage(mapping.TargetLanguage, packId, $"{path}.targetLanguage", errors);
        ValidateId(mapping.TargetConceptId, packId, $"{path}.targetConceptId", errors);
        if (mapping.SourceLanguage == mapping.TargetLanguage)
        {
            Add(errors, "mapping.language", packId, path, "Source and target languages must differ.");
        }

        if (!Enum.IsDefined(mapping.Relation))
        {
            Add(errors, "mapping.relation", packId, $"{path}.relation", "The transfer relation is invalid.");
        }

        if (!IsUnitInterval(mapping.Strength))
        {
            Add(errors, "mapping.strength", packId, $"{path}.strength", "Mapping strength must be between 0 and 1.");
        }

        ValidateTextList(mapping.BridgeConcepts, packId, $"{path}.bridgeConcepts", allowEmpty: mapping.Relation is TransferRelation.Neutral or TransferRelation.Unknown, errors);
        ValidateInstructionText(
            mapping.LearnerExplanation,
            packId,
            $"{path}.learnerExplanation",
            instructionLanguages,
            errors);
        RequireText(mapping.TeacherNotes, "mapping.notes", packId, $"{path}.teacherNotes", "Authoring notes are required.", errors);
        ValidateExamples(
            mapping.PositiveExamples,
            packId,
            $"{path}.positiveExamples",
            allowEmpty: mapping.Relation is TransferRelation.Neutral or TransferRelation.Unknown,
            instructionLanguages,
            errors);
        ValidateInstructionTextList(
            mapping.NegativeTransferRisks,
            packId,
            $"{path}.negativeTransferRisks",
            allowEmpty: mapping.Relation == TransferRelation.Facilitative,
            instructionLanguages,
            errors);
        ValidateSourceIds(mapping.SourceIds, packId, $"{path}.sourceIds", errors);
        ValidateReview(mapping.Review, packId, $"{path}.review", policy, errors);
    }

    private static void ValidatePackReferences(
        ContentPackDocument pack,
        IReadOnlyDictionary<string, (TargetConceptContent Item, string PackId)> concepts,
        IReadOnlyDictionary<string, (TaskTemplateContent Item, string PackId)> tasks,
        IReadOnlyDictionary<string, (ErrorRuleContent Item, string PackId)> errorRules,
        IReadOnlyDictionary<string, (FeedbackTemplateContent Item, string PackId)> feedback,
        IReadOnlyDictionary<string, ContentPackDocument> packsById,
        ICollection<ContentValidationError> errors)
    {
        var packId = pack.Manifest.Id;
        var sources = UniqueStrings(Items(pack.Sources).Where(source => source is not null).Select(source => source.Id));
        var taskEvaluators = Items(pack.Tasks)
            .Where(task => task is not null)
            .SelectMany(task => Items(task.Evaluators))
            .Where(evaluator => evaluator is not null)
            .Select(evaluator => evaluator.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var (concept, index) in Items(pack.Concepts).Select((item, index) => (item, index)))
        {
            if (concept is null)
            {
                continue;
            }

            var path = $"concepts[{index}]";
            RequireReferences(concept.PrerequisiteIds, concepts, packId, $"{path}.prerequisiteIds", "concept", errors);
            RequireReferences(concept.ErrorRuleIds, errorRules, packId, $"{path}.errorRuleIds", "error rule", errors);
            RequireSources(concept.SourceIds, sources, packId, $"{path}.sourceIds", errors);
            foreach (var evaluatorId in Items(concept.SuccessCriteria?.RequiredEvaluatorIds).Where(id => !taskEvaluators.Contains(id)))
            {
                Add(errors, "evaluator.coverage", packId, $"{path}.successCriteria.requiredEvaluatorIds", $"Evaluator '{evaluatorId}' does not resolve in this pack.");
            }
        }

        foreach (var (entry, index) in Items(pack.Lexicon).Select((item, index) => (item, index)))
        {
            if (entry is null)
            {
                continue;
            }

            RequireReferences(entry.ConceptIds, concepts, packId, $"lexicon[{index}].conceptIds", "concept", errors);
            RequireSources(entry.SourceIds, sources, packId, $"lexicon[{index}].sourceIds", errors);
        }

        foreach (var (task, index) in Items(pack.Tasks).Select((item, index) => (item, index)))
        {
            if (task is null)
            {
                continue;
            }

            var path = $"tasks[{index}]";
            RequireReferences(task.RequiredFunctionIds, concepts, packId, $"{path}.requiredFunctionIds", "concept", errors);
            RequireReferences(task.EligibleConceptIds, concepts, packId, $"{path}.eligibleConceptIds", "concept", errors);
            RequireSources(task.SourceIds, sources, packId, $"{path}.sourceIds", errors);
        }

        foreach (var (rule, index) in Items(pack.ErrorRules).Select((item, index) => (item, index)))
        {
            if (rule is null)
            {
                continue;
            }

            RequireReference(rule.TargetConceptId, concepts, packId, $"errorRules[{index}].targetConceptId", "concept", errors);
            RequireReference(rule.FeedbackTemplateId, feedback, packId, $"errorRules[{index}].feedbackTemplateId", "feedback template", errors);
            RequireSources(rule.SourceIds, sources, packId, $"errorRules[{index}].sourceIds", errors);
        }

        foreach (var (template, index) in Items(pack.FeedbackTemplates).Select((item, index) => (item, index)))
        {
            if (template is not null)
            {
                RequireSources(template.SourceIds, sources, packId, $"feedbackTemplates[{index}].sourceIds", errors);
            }
        }

        foreach (var (rubric, index) in Items(pack.Rubrics).Select((item, index) => (item, index)))
        {
            if (rubric is null)
            {
                continue;
            }

            RequireReference(rubric.TaskId, tasks, packId, $"rubrics[{index}].taskId", "task", errors);
            if (tasks.TryGetValue(rubric.TaskId, out var task))
            {
                var evaluators = Items(task.Item.Evaluators)
                    .Where(evaluator => evaluator is not null)
                    .Select(evaluator => evaluator.Id)
                    .ToHashSet(StringComparer.Ordinal);
                foreach (var (dimension, dimensionIndex) in Items(rubric.Dimensions).Select((item, dimensionIndex) => (item, dimensionIndex)))
                {
                    if (dimension is not null && !evaluators.Contains(dimension.EvaluatorId))
                    {
                        Add(errors, "evaluator.coverage", packId, $"rubrics[{index}].dimensions[{dimensionIndex}].evaluatorId", $"Evaluator '{dimension.EvaluatorId}' is not defined by task '{rubric.TaskId}'.");
                    }
                }
            }

            RequireSources(rubric.SourceIds, sources, packId, $"rubrics[{index}].sourceIds", errors);
        }

        foreach (var (utterance, index) in Items(pack.PronunciationUtterances).Select((item, index) => (item, index)))
        {
            if (utterance is null)
            {
                continue;
            }

            RequireReferences(utterance.ConceptIds, concepts, packId, $"pronunciationUtterances[{index}].conceptIds", "concept", errors);
            RequireSources(utterance.SourceIds, sources, packId, $"pronunciationUtterances[{index}].sourceIds", errors);
        }

        var dependencyIds = Items(pack.Manifest.Dependencies)
            .Where(dependency => dependency is not null)
            .Select(dependency => dependency.PackId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var (mapping, index) in Items(pack.TransferMappings).Select((item, index) => (item, index)))
        {
            if (mapping is null)
            {
                continue;
            }

            var path = $"transferMappings[{index}]";
            RequireReference(mapping.TargetConceptId, concepts, packId, $"{path}.targetConceptId", "concept", errors);
            if (concepts.TryGetValue(mapping.TargetConceptId, out var owner) && !dependencyIds.Contains(owner.PackId))
            {
                Add(errors, "dependency.missing", packId, $"{path}.targetConceptId", $"Pack '{owner.PackId}' owns the target concept but is not a declared dependency.");
            }

            RequireSources(mapping.SourceIds, sources, packId, $"{path}.sourceIds", errors);
        }

        if (pack.Manifest.Kind == ContentPackKind.TargetLanguage && Items(pack.Manifest.Languages).Count == 1)
        {
            var language = pack.Manifest.Languages[0];
            foreach (var (itemLanguage, path) in TargetLanguageItems(pack))
            {
                if (itemLanguage != language)
                {
                    Add(errors, "pack.language", packId, path, $"Item language '{itemLanguage}' does not match pack language '{language}'.");
                }
            }
        }

        if (pack.Manifest.Kind == ContentPackKind.Transfer && Items(pack.Manifest.Languages).Count == 2)
        {
            foreach (var (mapping, index) in Items(pack.TransferMappings).Select((item, index) => (item, index)))
            {
                if (mapping is null)
                {
                    continue;
                }

                if (mapping.SourceLanguage != pack.Manifest.Languages[0] ||
                    mapping.TargetLanguage != pack.Manifest.Languages[1])
                {
                    Add(errors, "pack.language", packId, $"transferMappings[{index}]", "Mapping languages do not match the manifest's ordered source and target languages.");
                }
            }
        }

        _ = packsById;
    }

    private static IEnumerable<(string Language, string Path)> TargetLanguageItems(ContentPackDocument pack)
    {
        foreach (var (concept, index) in Items(pack.Concepts).Select((item, index) => (item, index)))
        {
            if (concept is not null)
            {
                yield return (concept.Language, $"concepts[{index}].language");
            }
        }

        foreach (var (entry, index) in Items(pack.Lexicon).Select((item, index) => (item, index)))
        {
            if (entry is not null)
            {
                yield return (entry.Language, $"lexicon[{index}].language");
            }
        }

        foreach (var (task, index) in Items(pack.Tasks).Select((item, index) => (item, index)))
        {
            if (task is not null)
            {
                yield return (task.Language, $"tasks[{index}].language");
            }
        }

        foreach (var (template, index) in Items(pack.FeedbackTemplates).Select((item, index) => (item, index)))
        {
            if (template is not null)
            {
                yield return (template.Language, $"feedbackTemplates[{index}].language");
            }
        }

        foreach (var (utterance, index) in Items(pack.PronunciationUtterances).Select((item, index) => (item, index)))
        {
            if (utterance is not null)
            {
                yield return (utterance.Language, $"pronunciationUtterances[{index}].language");
            }
        }
    }

    private static void ValidateGlobalIds(
        IReadOnlyList<ContentPackDocument> packs,
        ICollection<ContentValidationError> errors)
    {
        var items = packs.SelectMany(pack => TopLevelIds(pack)).ToArray();
        foreach (var duplicate in items
                     .Where(item => !string.IsNullOrWhiteSpace(item.Id))
                     .GroupBy(item => item.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            foreach (var item in duplicate)
            {
                Add(errors, "id.duplicate", item.PackId, item.Path, $"Content ID '{item.Id}' appears more than once.");
            }
        }
    }

    private static IEnumerable<(string Id, string PackId, string Path)> TopLevelIds(ContentPackDocument pack)
    {
        var packId = pack.Manifest.Id;
        foreach (var (source, index) in Items(pack.Sources).Select((item, index) => (item, index)))
        {
            if (source is not null)
            {
                yield return (source.Id, packId, $"sources[{index}].id");
            }
        }

        foreach (var (concept, index) in Items(pack.Concepts).Select((item, index) => (item, index)))
        {
            if (concept is not null)
            {
                yield return (concept.Id, packId, $"concepts[{index}].id");
            }
        }

        foreach (var (entry, index) in Items(pack.Lexicon).Select((item, index) => (item, index)))
        {
            if (entry is not null)
            {
                yield return (entry.Id, packId, $"lexicon[{index}].id");
            }
        }

        foreach (var (task, index) in Items(pack.Tasks).Select((item, index) => (item, index)))
        {
            if (task is not null)
            {
                yield return (task.Id, packId, $"tasks[{index}].id");
            }
        }

        foreach (var (rule, index) in Items(pack.ErrorRules).Select((item, index) => (item, index)))
        {
            if (rule is not null)
            {
                yield return (rule.Id, packId, $"errorRules[{index}].id");
            }
        }

        foreach (var (template, index) in Items(pack.FeedbackTemplates).Select((item, index) => (item, index)))
        {
            if (template is not null)
            {
                yield return (template.Id, packId, $"feedbackTemplates[{index}].id");
            }
        }

        foreach (var (rubric, index) in Items(pack.Rubrics).Select((item, index) => (item, index)))
        {
            if (rubric is not null)
            {
                yield return (rubric.Id, packId, $"rubrics[{index}].id");
            }
        }

        foreach (var (utterance, index) in Items(pack.PronunciationUtterances).Select((item, index) => (item, index)))
        {
            if (utterance is not null)
            {
                yield return (utterance.Id, packId, $"pronunciationUtterances[{index}].id");
            }
        }

        foreach (var (example, path) in PackExamples(pack))
        {
            if (example?.Id is { } exampleId)
            {
                yield return (exampleId, packId, $"{path}.id");
            }
        }

        foreach (var (lesson, lessonIndex) in Items(pack.Lessons).Select((item, index) => (item, index)))
        {
            if (lesson is null)
            {
                continue;
            }

            yield return (lesson.Id, packId, $"lessons[{lessonIndex}].id");
            foreach (var (instance, instanceIndex) in Items(lesson.TemplateInstances).Select((item, index) => (item, index)))
            {
                if (instance is not null)
                {
                    yield return (
                        instance.Id,
                        packId,
                        $"lessons[{lessonIndex}].templateInstances[{instanceIndex}].id");
                }
            }
        }

        foreach (var (mapping, index) in Items(pack.TransferMappings).Select((item, index) => (item, index)))
        {
            if (mapping is not null)
            {
                yield return (mapping.Id, packId, $"transferMappings[{index}].id");
            }
        }
    }

    private static IEnumerable<string> NamedExampleIds(ContentPackDocument pack) =>
        PackExamples(pack)
            .Where(item => item.Example?.Id is not null)
            .Select(item => item.Example.Id!);

    private static IEnumerable<(ContentExample Example, string Path)> PackExamples(ContentPackDocument pack)
    {
        foreach (var (concept, conceptIndex) in Items(pack.Concepts).Select((item, index) => (item, index)))
        {
            if (concept is null)
            {
                continue;
            }

            foreach (var (example, exampleIndex) in Items(concept.Examples).Select((item, index) => (item, index)))
            {
                if (example is not null)
                {
                    yield return (example, $"concepts[{conceptIndex}].examples[{exampleIndex}]");
                }
            }

            foreach (var (example, exampleIndex) in Items(concept.Counterexamples).Select((item, index) => (item, index)))
            {
                if (example is not null)
                {
                    yield return (example, $"concepts[{conceptIndex}].counterexamples[{exampleIndex}]");
                }
            }
        }

        foreach (var (entry, entryIndex) in Items(pack.Lexicon).Select((item, index) => (item, index)))
        {
            if (entry is null)
            {
                continue;
            }

            foreach (var (example, exampleIndex) in Items(entry.Examples).Select((item, index) => (item, index)))
            {
                if (example is not null)
                {
                    yield return (example, $"lexicon[{entryIndex}].examples[{exampleIndex}]");
                }
            }
        }

        foreach (var (mapping, mappingIndex) in Items(pack.TransferMappings).Select((item, index) => (item, index)))
        {
            if (mapping is null)
            {
                continue;
            }

            foreach (var (example, exampleIndex) in Items(mapping.PositiveExamples).Select((item, index) => (item, index)))
            {
                if (example is not null)
                {
                    yield return (example, $"transferMappings[{mappingIndex}].positiveExamples[{exampleIndex}]");
                }
            }
        }
    }

    private static void ValidateDependenciesShape(
        IReadOnlyList<PackDependency> dependencies,
        string packId,
        ICollection<ContentValidationError> errors)
    {
        if (dependencies is null)
        {
            Add(errors, "dependency.collection", packId, "manifest.dependencies", "The dependency collection is missing.");
            return;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (dependency, index) in dependencies.Select((dependency, index) => (dependency, index)))
        {
            var path = $"manifest.dependencies[{index}]";
            if (dependency is null)
            {
                Add(errors, "dependency.missing", packId, path, "A dependency is missing.");
                continue;
            }

            ValidateId(dependency.PackId, packId, $"{path}.packId", errors);
            if (!ids.Add(dependency.PackId))
            {
                Add(errors, "id.duplicate", packId, $"{path}.packId", $"Dependency '{dependency.PackId}' appears more than once.");
            }

            if (dependency.MinimumVersion < 1 || dependency.MaximumVersion < dependency.MinimumVersion)
            {
                Add(errors, "dependency.version", packId, path, "The dependency version range is invalid.");
            }
        }
    }

    private static void ValidateDependencies(
        IEnumerable<ContentPackDocument> packs,
        IReadOnlyDictionary<string, ContentPackDocument> packsById,
        ICollection<ContentValidationError> errors)
    {
        foreach (var pack in packs)
        {
            foreach (var (dependency, index) in Items(pack.Manifest.Dependencies).Select((item, index) => (item, index)))
            {
                if (dependency is null)
                {
                    continue;
                }

                var path = $"manifest.dependencies[{index}]";
                if (!packsById.TryGetValue(dependency.PackId, out var installed))
                {
                    Add(errors, "dependency.missing", pack.Manifest.Id, path, $"Required pack '{dependency.PackId}' is not installed.");
                }
                else if (installed.Manifest.Version < dependency.MinimumVersion ||
                         installed.Manifest.Version > dependency.MaximumVersion)
                {
                    Add(
                        errors,
                        "dependency.conflict",
                        pack.Manifest.Id,
                        path,
                        $"Pack '{dependency.PackId}' version {installed.Manifest.Version} is outside {dependency.MinimumVersion}-{dependency.MaximumVersion}.");
                }
            }
        }
    }

    private static void ValidateConceptCycles(
        IReadOnlyDictionary<string, (TargetConceptContent Item, string PackId)> concepts,
        ICollection<ContentValidationError> errors)
    {
        var states = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        foreach (var id in concepts.Keys.Order(StringComparer.Ordinal))
        {
            Visit(id, concepts, states, new Stack<string>(), errors);
        }
    }

    private static void Visit(
        string id,
        IReadOnlyDictionary<string, (TargetConceptContent Item, string PackId)> concepts,
        IDictionary<string, VisitState> states,
        Stack<string> path,
        ICollection<ContentValidationError> errors)
    {
        if (states.TryGetValue(id, out var state))
        {
            if (state == VisitState.Visiting)
            {
                var cycle = path.Reverse().SkipWhile(item => item != id).Append(id);
                Add(
                    errors,
                    "graph.cycle",
                    concepts[id].PackId,
                    $"concepts.{id}.prerequisiteIds",
                    $"Concept prerequisite cycle: {string.Join(" -> ", cycle)}.");
            }

            return;
        }

        states[id] = VisitState.Visiting;
        path.Push(id);
        foreach (var prerequisite in Items(concepts[id].Item.PrerequisiteIds)
                     .Where(concepts.ContainsKey)
                     .Order(StringComparer.Ordinal))
        {
            Visit(prerequisite, concepts, states, path, errors);
        }

        path.Pop();
        states[id] = VisitState.Visited;
    }

    private static void ValidateReview(
        ContentReview review,
        string packId,
        string path,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        if (review is null)
        {
            Add(errors, "review.missing", packId, path, "Review metadata is required.");
            return;
        }

        if (!Enum.IsDefined(review.Status))
        {
            Add(errors, "review.status", packId, $"{path}.status", "The review status is invalid.");
            return;
        }

        RequireText(review.Notes, "review.notes", packId, $"{path}.notes", "Review notes are required.", errors);
        if (review.Status is ContentReviewStatus.LinguisticallyReviewed or ContentReviewStatus.Approved &&
            (string.IsNullOrWhiteSpace(review.Reviewer) || review.ReviewedOn is null))
        {
            Add(errors, "review.attribution", packId, path, "Linguistic review and approval require a named reviewer and date.");
        }

        var eligible = policy switch
        {
            ContentLoadPolicy.ValidationOnly => review.Status != ContentReviewStatus.Rejected,
            ContentLoadPolicy.AuthoringPreview => review.Status is
                ContentReviewStatus.MachineValidated or
                ContentReviewStatus.LinguisticallyReviewed or
                ContentReviewStatus.Approved,
            ContentLoadPolicy.Runtime => review.Status == ContentReviewStatus.Approved,
            _ => false,
        };
        if (!eligible)
        {
            Add(errors, "review.ineligible", packId, $"{path}.status", $"Review status '{review.Status}' is not eligible for {policy}.");
        }
    }

    private static void ValidateLicense(
        ContentLicense license,
        string packId,
        string path,
        ContentLoadPolicy policy,
        ICollection<ContentValidationError> errors)
    {
        if (license is null)
        {
            Add(errors, "license.missing", packId, path, "License metadata is required.");
            return;
        }

        RequireText(license.Identifier, "license.field", packId, $"{path}.identifier", "A license identifier is required.", errors);
        RequireText(license.CopyrightHolder, "license.field", packId, $"{path}.copyrightHolder", "A copyright holder is required.", errors);
        RequireText(license.LicenseTextLocation, "license.field", packId, $"{path}.licenseTextLocation", "A license text location is required.", errors);
        RequireText(license.IntendedUse, "license.field", packId, $"{path}.intendedUse", "The intended use is required.", errors);
        RequireText(license.RequiredAttribution, "license.field", packId, $"{path}.requiredAttribution", "Required attribution must be explicit, including 'None'.", errors);
        if (!Enum.IsDefined(license.ReviewStatus))
        {
            Add(errors, "license.status", packId, $"{path}.reviewStatus", "The license review status is invalid.");
        }
        else if (license.ReviewStatus == LicenseReviewStatus.Rejected ||
                 policy == ContentLoadPolicy.Runtime && license.ReviewStatus != LicenseReviewStatus.Reviewed)
        {
            Add(errors, "license.unreviewed", packId, $"{path}.reviewStatus", $"License status '{license.ReviewStatus}' is not eligible for {policy}.");
        }
    }

    private static void ValidateLanguages(
        IReadOnlyList<string> languages,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (languages is null || languages.Count == 0)
        {
            Add(errors, "language.missing", packId, path, "At least one language is required.");
            return;
        }

        foreach (var (language, index) in languages.Select((language, index) => (language, index)))
        {
            ValidateLanguage(language, packId, $"{path}[{index}]", errors);
        }

        if (languages.Count != languages.Distinct(StringComparer.Ordinal).Count())
        {
            Add(errors, "id.duplicate", packId, path, "A manifest repeats a language.");
        }
    }

    private static void ValidateLanguage(
        string language,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        try
        {
            var normalized = new LanguageCode(language).Value;
            if (!string.Equals(language, normalized, StringComparison.Ordinal))
            {
                Add(errors, "language.invalid", packId, path, $"Language code '{language}' is not in canonical lower-case form.");
            }
        }
        catch (ArgumentException)
        {
            Add(errors, "language.invalid", packId, path, $"Language code '{language}' is invalid.");
        }
    }

    private static void ValidateCefr(
        string cefr,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (!AllowedCefr.Contains(cefr))
        {
            Add(errors, "cefr.invalid", packId, path, $"CEFR approximation '{cefr}' is invalid.");
        }
    }

    private static void ValidateId(
        string id,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        if (!IsCanonicalIdentifier(id))
        {
            Add(errors, "id.invalid", packId, path, $"Identifier '{id}' is missing or not canonical.");
        }
    }

    private static bool IsCanonicalIdentifier(string id)
    {
        try
        {
            return string.Equals(
                id,
                CurriculumIdentifier.Normalize(id, nameof(id)),
                StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void ValidateIdList(
        IReadOnlyList<string> ids,
        string packId,
        string path,
        ICollection<ContentValidationError> errors,
        bool allowEmpty = true)
    {
        if (ids is null)
        {
            Add(errors, "collection.missing", packId, path, "The identifier collection is missing.");
            return;
        }

        if (!allowEmpty && ids.Count == 0)
        {
            Add(errors, "collection.empty", packId, path, "At least one identifier is required.");
        }

        foreach (var (id, index) in ids.Select((id, index) => (id, index)))
        {
            ValidateId(id, packId, $"{path}[{index}]", errors);
        }

        if (ids.Count != ids.Distinct(StringComparer.Ordinal).Count())
        {
            Add(errors, "id.duplicate", packId, path, "The identifier collection contains a duplicate.");
        }
    }

    private static void ValidateSourceIds(
        IReadOnlyList<string> ids,
        string packId,
        string path,
        ICollection<ContentValidationError> errors) =>
        ValidateIdList(ids, packId, path, errors, allowEmpty: false);

    private static void ValidateTextList(
        IReadOnlyList<string> values,
        string packId,
        string path,
        bool allowEmpty,
        ICollection<ContentValidationError> errors)
    {
        if (values is null)
        {
            Add(errors, "collection.missing", packId, path, "The text collection is missing.");
            return;
        }

        if (!allowEmpty && values.Count == 0)
        {
            Add(errors, "collection.empty", packId, path, "At least one value is required.");
        }

        foreach (var (value, index) in values.Select((value, index) => (value, index)))
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(errors, "text.missing", packId, $"{path}[{index}]", "A text value is missing.");
            }
        }
    }

    private static void ValidateInstructionText(
        IReadOnlyDictionary<string, string> values,
        string packId,
        string path,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        if (values is null || values.Count == 0)
        {
            Add(errors, "explanation.missing", packId, path, "Instruction text is required.");
            return;
        }

        foreach (var (language, value) in values)
        {
            ValidateLanguage(language, packId, $"{path}.{language}", errors);
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(
                    errors,
                    "explanation.missing",
                    packId,
                    $"{path}.{language}",
                    $"Instruction text for '{language}' is required.");
            }
        }

        foreach (var language in instructionLanguages.Where(language =>
                     !values.TryGetValue(language, out var value) || string.IsNullOrWhiteSpace(value)))
        {
            Add(
                errors,
                "instruction.coverage",
                packId,
                path,
                $"Instruction text for declared language '{language}' is missing.");
        }
    }

    private static void ValidateInstructionTextList(
        IReadOnlyDictionary<string, IReadOnlyList<string>> values,
        string packId,
        string path,
        bool allowEmpty,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        if (values is null || values.Count == 0)
        {
            Add(errors, "explanation.missing", packId, path, "Instruction text is required.");
            return;
        }

        foreach (var (language, items) in values)
        {
            ValidateLanguage(language, packId, $"{path}.{language}", errors);
            ValidateTextList(items, packId, $"{path}.{language}", allowEmpty, errors);
        }

        foreach (var language in instructionLanguages.Where(language => !values.ContainsKey(language)))
        {
            Add(
                errors,
                "instruction.coverage",
                packId,
                path,
                $"Instruction text for declared language '{language}' is missing.");
        }
    }

    private static void ValidateExamples(
        IReadOnlyList<ContentExample> examples,
        string packId,
        string path,
        bool allowEmpty,
        IReadOnlySet<string> instructionLanguages,
        ICollection<ContentValidationError> errors)
    {
        if (examples is null)
        {
            Add(errors, "collection.missing", packId, path, "The example collection is missing.");
            return;
        }

        if (!allowEmpty && examples.Count == 0)
        {
            Add(errors, "collection.empty", packId, path, "At least one example is required.");
        }

        foreach (var (example, index) in examples.Select((example, index) => (example, index)))
        {
            if (example is null || string.IsNullOrWhiteSpace(example.Text))
            {
                Add(errors, "example.invalid", packId, $"{path}[{index}]", "An example needs target-language text.");
            }
            else
            {
                ValidateInstructionText(
                    example.Meaning,
                    packId,
                    $"{path}[{index}].meaning",
                    instructionLanguages,
                    errors);
                ValidateInstructionText(
                    example.Note,
                    packId,
                    $"{path}[{index}].note",
                    instructionLanguages,
                    errors);

                if (example.Id is { } exampleId)
                {
                    ValidateId(exampleId, packId, $"{path}[{index}].id", errors);
                }
            }
        }
    }

    private static void RequireSources(
        IReadOnlyList<string> sourceIds,
        IReadOnlySet<string> sources,
        string packId,
        string path,
        ICollection<ContentValidationError> errors)
    {
        foreach (var sourceId in Items(sourceIds).Where(sourceId => !sources.Contains(sourceId)))
        {
            Add(errors, "provenance.missing", packId, path, $"Source '{sourceId}' does not resolve in this pack.");
        }
    }

    private static void RequireReferences<T>(
        IReadOnlyList<string> ids,
        IReadOnlyDictionary<string, (T Item, string PackId)> known,
        string packId,
        string path,
        string kind,
        ICollection<ContentValidationError> errors)
    {
        foreach (var id in Items(ids))
        {
            RequireReference(id, known, packId, path, kind, errors);
        }
    }

    private static void RequireReference<T>(
        string id,
        IReadOnlyDictionary<string, (T Item, string PackId)> known,
        string packId,
        string path,
        string kind,
        ICollection<ContentValidationError> errors)
    {
        if (!known.ContainsKey(id))
        {
            Add(errors, "reference.broken", packId, path, $"Referenced {kind} '{id}' does not resolve.");
        }
    }

    private static IReadOnlyDictionary<string, (T Item, string PackId)> UniqueItems<T>(
        IEnumerable<(string Id, T Item, string PackId)> items,
        IEqualityComparer<string> comparer) =>
        items
            .Where(item => item.Item is not null && !string.IsNullOrWhiteSpace(item.Id))
            .GroupBy(item => item.Id, comparer)
            .Where(group => group.Count() == 1)
            .ToDictionary(
                group => group.Key,
                group => (group.Single().Item, group.Single().PackId),
                comparer);

    private static IReadOnlySet<string> UniqueStrings(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<T> Items<T>(IReadOnlyList<T>? values) => values ?? [];

    private static bool IsUnitInterval(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value) && value is >= 0 and <= 1;

    private static string TextOrFallback(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static void RequireText(
        string value,
        string code,
        string packId,
        string path,
        string message,
        ICollection<ContentValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(errors, code, packId, path, message);
        }
    }

    private static void Add(
        ICollection<ContentValidationError> errors,
        string code,
        string packId,
        string path,
        string message) =>
        errors.Add(new ContentValidationError(code, packId, path, message));

    private static void AddTemplate(
        ICollection<ContentValidationError> errors,
        string code,
        string packId,
        string path,
        string lessonId,
        string parameter,
        string message) =>
        errors.Add(new ContentValidationError(
            code,
            packId,
            path,
            message,
            lessonId,
            parameter));

    private static IReadOnlyList<ContentValidationError> Order(
        IEnumerable<ContentValidationError> errors) =>
        errors
            .OrderBy(error => error.PackId, StringComparer.Ordinal)
            .ThenBy(error => error.Path, StringComparer.Ordinal)
            .ThenBy(error => error.Code, StringComparer.Ordinal)
            .ThenBy(error => error.Message, StringComparer.Ordinal)
            .ToArray();

    private enum VisitState
    {
        Visiting,
        Visited,
    }
}
