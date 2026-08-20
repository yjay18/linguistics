using System.Text.Json;
using System.Text.Json.Serialization;
using Linguistics.Core.Content;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class ContentPackTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false),
        },
    };

    [TestMethod]
    public void BundledPacksDecodeAndValidateForAuthoringPreview()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);

        Assert.AreEqual(ContentLoadPolicy.AuthoringPreview, catalog.Policy);
        Assert.HasCount(3, catalog.Packs);
        var german = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        Assert.HasCount(13, german.Concepts);
        Assert.HasCount(4, german.Tasks);
        Assert.HasCount(5, german.ErrorRules);
        Assert.HasCount(4, german.Rubrics);
        Assert.HasCount(4, german.PronunciationUtterances);
        Assert.IsTrue(german.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
    }

    [TestMethod]
    public void TargetAndTransferOwnershipRemainIndependent()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var german = catalog.Packs.Single(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage);
        var transfers = catalog.Packs.Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer).ToArray();

        Assert.HasCount(13, german.Concepts);
        Assert.HasCount(2, transfers);
        Assert.IsTrue(transfers.All(pack => pack.Concepts.Count == 0));
        Assert.IsTrue(transfers.All(pack => pack.Manifest.Dependencies.Single().PackId == german.Manifest.Id));
    }

    [TestMethod]
    public void RequiredHelpfulInterferingAndNoBridgeDraftsExist()
    {
        var mappings = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.SelectMany(pack => pack.TransferMappings)
            .ToArray();

        Assert.IsTrue(mappings.Any(mapping =>
            mapping.SourceLanguage == "en" && mapping.Relation == TransferRelation.Facilitative));
        Assert.IsTrue(mappings.Any(mapping =>
            mapping.SourceLanguage == "hi" && mapping.Relation == TransferRelation.Facilitative));
        Assert.IsTrue(mappings.Any(mapping =>
            mapping.SourceLanguage == "en" && mapping.Relation == TransferRelation.Interfering));
        Assert.IsTrue(mappings.Any(mapping =>
            mapping.Relation is TransferRelation.Neutral or TransferRelation.Unknown));
        Assert.IsTrue(mappings.All(mapping => mapping.SourceIds.Count > 0));
        Assert.IsTrue(mappings.All(mapping => mapping.Review.Status == ContentReviewStatus.MachineValidated));
    }

    [TestMethod]
    public void FourTasksHaveReachableDeterministicSuccessContractsAndFallbacks()
    {
        var tasks = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.SelectMany(pack => pack.Tasks)
            .ToArray();

        Assert.HasCount(4, tasks);
        Assert.IsTrue(tasks.All(task => task.Transitions.Count > 0));
        Assert.IsTrue(tasks.All(task => task.SuccessConditions.Count > 0));
        Assert.IsTrue(tasks.All(task => task.States.All(state => state.ScriptedFallback.Count > 0)));
        Assert.IsTrue(tasks.All(task => task.Evaluators.All(evaluator =>
            Enum.IsDefined(evaluator.Kind))));
        var cafe = tasks.Single(task => task.Id == "de.task.cafe.order-one-item");
        Assert.IsTrue(cafe.Transitions.Any(transition =>
            transition.FromStateId == cafe.InitialStateId &&
            cafe.SuccessStateIds.Contains(transition.ToStateId, StringComparer.Ordinal) &&
            transition.EvaluatorId == "de.eval.order-full-request"));
    }

    [TestMethod]
    public void MachineValidatedDraftsCannotBecomeRuntimeTeachingObjects()
    {
        var exception = Assert.ThrowsExactly<ContentValidationException>(() =>
            LoadBundled(ContentLoadPolicy.Runtime));

        Assert.IsTrue(exception.Errors.Any(error => error.Code == "review.ineligible"));
        Assert.IsTrue(exception.Errors.Any(error => error.Code == "license.unreviewed"));
    }

    [TestMethod]
    public void OnlyAValidatedApprovedCatalogCreatesRuntimeTeachingObjects()
    {
        var approved = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Select(ApproveForTestOnly)
            .ToArray();
        var directory = WritePacks(approved);
        try
        {
            var runtime = ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.Runtime);
            var graph = runtime.CreateRuntimeConceptGraph(new LanguageCode("de"));
            var english = runtime.CreateRuntimeTransferMappings(
                new LanguageCode("en"),
                new LanguageCode("de"));
            var hindiNotes = runtime.CreateRuntimeTransferNotes(
                new LanguageCode("hi"),
                new LanguageCode("de"));
            var cafe = runtime.CreateRuntimeCafeOrderDefinition();
            var pronunciation = runtime.CreateRuntimePronunciationUtterances(
                new LanguageCode("de"));

            Assert.HasCount(13, graph.Nodes);
            Assert.HasCount(3, english);
            Assert.IsTrue(english.All(mapping => mapping.ReviewStatus == TransferReviewStatus.Approved));
            Assert.IsTrue(hindiNotes.Any(note =>
                note.Mapping.TargetConceptId == new ConceptId("de.noun.gender-basic")));
            Assert.AreEqual("de.task.cafe.order-one-item", cafe.TaskId);
            Assert.AreEqual(new ConceptId("de.function.order-polite"), cafe.TargetConceptId);
            Assert.IsNotEmpty(cafe.ScriptedResponses[cafe.CompleteStateId]);
            Assert.AreEqual("Ich möchte einen Kaffee, bitte.", cafe.PronunciationTargetText);
            Assert.HasCount(4, pronunciation);
            Assert.IsTrue(pronunciation.All(utterance =>
                utterance.ContentVersion == new VersionId("language.de.core.v1")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AuthoringCatalogCannotBypassRuntimeReviewPolicy()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            catalog.CreateRuntimeConceptGraph(new LanguageCode("de")));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            catalog.CreateRuntimeTransferMappings(new LanguageCode("en"), new LanguageCode("de")));
    }

    [TestMethod]
    public void RepeatedLoadsReturnTheSamePackAndItemOrder()
    {
        var first = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var second = LoadBundled(ContentLoadPolicy.AuthoringPreview);

        CollectionAssert.AreEqual(
            first.Packs.Select(pack => pack.Manifest.Id).ToArray(),
            second.Packs.Select(pack => pack.Manifest.Id).ToArray());
        CollectionAssert.AreEqual(
            first.Packs.SelectMany(pack => pack.Concepts).Select(concept => concept.Id).ToArray(),
            second.Packs.SelectMany(pack => pack.Concepts).Select(concept => concept.Id).ToArray());
    }

    [TestMethod]
    public void AuthoringContentBecomesDeterministicPreviewLessons()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .CreateCourseCatalog(new LanguageCode("de"));
        var repeated = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .CreateCourseCatalog(new LanguageCode("de"));

        Assert.AreEqual(CoursePublicationState.Preview, catalog.PublicationState);
        Assert.AreEqual(450, catalog.TargetLessonCount);
        Assert.AreEqual(13, catalog.AuthoredLessonCount);
        Assert.AreEqual(437, catalog.RemainingLessonCount);
        Assert.AreEqual("Greet someone", catalog.Units[0].Lessons[0].Title);
        Assert.IsTrue(catalog.Units.SelectMany(unit => unit.Lessons).All(lesson => lesson.Slides.Count >= 5));
        CollectionAssert.AreEqual(
            catalog.Units.SelectMany(unit => unit.Lessons).Select(lesson => lesson.Id).ToArray(),
            repeated.Units.SelectMany(unit => unit.Lessons).Select(lesson => lesson.Id).ToArray());
        CollectionAssert.AreEqual(
            catalog.Units.SelectMany(unit => unit.Lessons).SelectMany(lesson => lesson.Slides).Select(slide => slide.Id).ToArray(),
            repeated.Units.SelectMany(unit => unit.Lessons).SelectMany(lesson => lesson.Slides).Select(slide => slide.Id).ToArray());
    }

    [TestMethod]
    public void RuntimeApprovedContentBecomesReadyLessons()
    {
        var approved = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Select(ApproveForTestOnly)
            .ToArray();
        var directory = WritePacks(approved);
        try
        {
            var course = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.Runtime)
                .CreateCourseCatalog(new LanguageCode("de"));

            Assert.AreEqual(CoursePublicationState.Ready, course.PublicationState);
            Assert.IsTrue(course.Units.SelectMany(unit => unit.Lessons).All(lesson =>
                lesson.ReviewStatus == ContentReviewStatus.Approved));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void CatalogAcceptsFiveHundredLessonsAndRejectsMore()
    {
        var source = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var fiveHundred = CreateLargeTargetPack(source, 500);
        var tooManyErrors = ContentPackValidator.Validate(
            [CreateLargeTargetPack(source, 501)],
            ContentLoadPolicy.AuthoringPreview);
        var directory = WritePacks([fiveHundred]);
        try
        {
            var course = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview)
                .CreateCourseCatalog(new LanguageCode("de"));

            Assert.AreEqual(500, course.AuthoredLessonCount);
            Assert.HasCount(25, course.Units);
            Assert.IsTrue(course.Units.All(unit => unit.Lessons.Count == 20));
            Assert.IsTrue(tooManyErrors.Any(error => error.Code == "catalog.limit"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    [DataRow("invalid-id", "id.invalid")]
    [DataRow("duplicate-id", "id.duplicate")]
    [DataRow("cycle", "graph.cycle")]
    [DataRow("broken-reference", "reference.broken")]
    [DataRow("invalid-language", "language.invalid")]
    [DataRow("unsupported-schema", "schema.unsupported")]
    [DataRow("unsupported-version", "version.unsupported")]
    [DataRow("invalid-cefr", "cefr.invalid")]
    [DataRow("invalid-transition", "task.transition")]
    [DataRow("missing-evaluator", "evaluator.coverage")]
    [DataRow("missing-provenance", "provenance.missing")]
    [DataRow("ineligible-review", "review.ineligible")]
    [DataRow("missing-license", "license.field")]
    [DataRow("unreviewed-license", "license.unreviewed")]
    [DataRow("malformed-error-pattern", "error.pattern")]
    [DataRow("missing-explanation", "explanation.missing")]
    [DataRow("missing-dependency", "dependency.missing")]
    public void CorruptFixturesFailWithAttributableErrors(string corruption, string expectedCode)
    {
        var (packs, policy) = Corrupt(corruption);

        var errors = ContentPackValidator.Validate(packs, policy);
        var error = errors.FirstOrDefault(candidate => candidate.Code == expectedCode);

        Assert.IsNotNull(error, string.Join(Environment.NewLine, errors));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error.PackId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error.Path));
        Assert.IsFalse(string.IsNullOrWhiteSpace(error.Message));
    }

    [TestMethod]
    public void MalformedJsonReportsItsFileAndJsonPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"linguistics-content-{Guid.NewGuid():N}");
        var packDirectory = Path.Combine(directory, "broken");
        Directory.CreateDirectory(packDirectory);
        File.WriteAllText(Path.Combine(packDirectory, "pack.json"), "{\"manifest\": {\"id\": }}");
        try
        {
            var exception = Assert.ThrowsExactly<ContentValidationException>(() =>
                ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.ValidationOnly));

            var error = exception.Errors.Single();
            Assert.AreEqual("decode.json", error.Code);
            StringAssert.Contains(error.PackId, "broken");
            Assert.IsFalse(string.IsNullOrWhiteSpace(error.Path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static (IReadOnlyList<ContentPackDocument> Packs, ContentLoadPolicy Policy) Corrupt(
        string corruption)
    {
        var packs = LoadBundled(ContentLoadPolicy.AuthoringPreview).Packs.ToArray();
        var targetIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "language.de.core");
        var target = packs[targetIndex];
        var englishIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "transfer.en-de.core");
        var english = packs[englishIndex];
        var policy = ContentLoadPolicy.ValidationOnly;

        switch (corruption)
        {
            case "invalid-id":
                target = ReplaceConcept(target, 0, target.Concepts[0] with { Id = "" });
                break;
            case "duplicate-id":
                target = ReplaceConcept(target, 1, target.Concepts[1] with { Id = target.Concepts[0].Id });
                break;
            case "cycle":
                target = ReplaceConcept(
                    target,
                    0,
                    target.Concepts[0] with { PrerequisiteIds = [target.Concepts[0].Id] });
                break;
            case "broken-reference":
                target = ReplaceConcept(
                    target,
                    0,
                    target.Concepts[0] with { PrerequisiteIds = ["de.concept.missing"] });
                break;
            case "invalid-language":
                target = ReplaceConcept(target, 0, target.Concepts[0] with { Language = "german" });
                break;
            case "unsupported-schema":
                target = target with { Manifest = target.Manifest with { SchemaVersion = 99 } };
                break;
            case "unsupported-version":
                target = target with { Manifest = target.Manifest with { Version = 99 } };
                break;
            case "invalid-cefr":
                target = ReplaceConcept(target, 0, target.Concepts[0] with { CefrApproximation = "A0" });
                break;
            case "invalid-transition":
                var transition = target.Tasks[0].Transitions[0] with { ToStateId = "de.state.missing" };
                target = ReplaceTask(
                    target,
                    0,
                    target.Tasks[0] with { Transitions = Replace(target.Tasks[0].Transitions, 0, transition) });
                break;
            case "missing-evaluator":
                var condition = target.Tasks[0].SuccessConditions[0] with { EvaluatorId = "de.eval.missing" };
                target = ReplaceTask(
                    target,
                    0,
                    target.Tasks[0] with { SuccessConditions = Replace(target.Tasks[0].SuccessConditions, 0, condition) });
                break;
            case "missing-provenance":
                target = ReplaceConcept(
                    target,
                    0,
                    target.Concepts[0] with { SourceIds = ["source.de.missing"] });
                break;
            case "ineligible-review":
                policy = ContentLoadPolicy.AuthoringPreview;
                target = ReplaceConcept(
                    target,
                    0,
                    target.Concepts[0] with
                    {
                        Review = target.Concepts[0].Review with { Status = ContentReviewStatus.Draft },
                    });
                break;
            case "missing-license":
                target = target with
                {
                    Manifest = target.Manifest with
                    {
                        License = target.Manifest.License with { Identifier = "" },
                    },
                };
                break;
            case "unreviewed-license":
                policy = ContentLoadPolicy.Runtime;
                break;
            case "malformed-error-pattern":
                var rule = target.ErrorRules[1] with
                {
                    Pattern = target.ErrorRules[1].Pattern with { Values = ["aus", "von"] },
                };
                target = target with { ErrorRules = Replace(target.ErrorRules, 1, rule) };
                break;
            case "missing-explanation":
                english = english with
                {
                    TransferMappings = Replace(
                        english.TransferMappings,
                        0,
                        english.TransferMappings[0] with { LearnerExplanation = "" }),
                };
                break;
            case "missing-dependency":
                english = english with
                {
                    Manifest = english.Manifest with
                    {
                        Dependencies =
                        [
                            english.Manifest.Dependencies[0] with
                            {
                                PackId = "language.de.missing",
                            },
                        ],
                    },
                };
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(corruption));
        }

        packs[targetIndex] = target;
        packs[englishIndex] = english;
        return (packs, policy);
    }

    private static ContentPackDocument ReplaceConcept(
        ContentPackDocument pack,
        int index,
        TargetConceptContent concept) =>
        pack with { Concepts = Replace(pack.Concepts, index, concept) };

    private static ContentPackDocument ReplaceTask(
        ContentPackDocument pack,
        int index,
        TaskTemplateContent task) =>
        pack with { Tasks = Replace(pack.Tasks, index, task) };

    private static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int index, T replacement)
    {
        var result = items.ToArray();
        result[index] = replacement;
        return result;
    }

    private static ContentPackDocument ApproveForTestOnly(ContentPackDocument pack)
    {
        var review = new ContentReview(
            ContentReviewStatus.Approved,
            "Synthetic test reviewer",
            new DateOnly(2026, 8, 19),
            "Test-only fixture proving the runtime gate; not approval of bundled content.");
        ContentLicense ApprovedLicense(ContentLicense license) =>
            license with { ReviewStatus = LicenseReviewStatus.Reviewed };

        return pack with
        {
            Manifest = pack.Manifest with
            {
                License = ApprovedLicense(pack.Manifest.License),
                Review = review,
            },
            Sources = pack.Sources
                .Select(source => source with { License = ApprovedLicense(source.License) })
                .ToArray(),
            Concepts = pack.Concepts.Select(item => item with { Review = review }).ToArray(),
            Lexicon = pack.Lexicon.Select(item => item with { Review = review }).ToArray(),
            Tasks = pack.Tasks.Select(item => item with { Review = review }).ToArray(),
            ErrorRules = pack.ErrorRules.Select(item => item with { Review = review }).ToArray(),
            FeedbackTemplates = pack.FeedbackTemplates.Select(item => item with { Review = review }).ToArray(),
            Rubrics = pack.Rubrics.Select(item => item with { Review = review }).ToArray(),
            PronunciationUtterances = pack.PronunciationUtterances
                .Select(item => item with { Review = review })
                .ToArray(),
            TransferMappings = pack.TransferMappings.Select(item => item with { Review = review }).ToArray(),
        };
    }

    private static ContentPackDocument CreateLargeTargetPack(
        ContentPackDocument source,
        int lessonCount)
    {
        var seed = source.Concepts[0];
        var concepts = Enumerable.Range(1, lessonCount)
            .Select(index => seed with
            {
                Id = $"de.synthetic.lesson{index:000}",
                Title = $"Synthetic lesson {index}",
                PrerequisiteIds = [],
                SuccessCriteria = seed.SuccessCriteria with { RequiredEvaluatorIds = [] },
                ErrorRuleIds = [],
            })
            .ToArray();

        return source with
        {
            Concepts = concepts,
            Lexicon = [],
            Tasks = [],
            ErrorRules = [],
            FeedbackTemplates = [],
            Rubrics = [],
            PronunciationUtterances = [],
        };
    }

    private static string WritePacks(IEnumerable<ContentPackDocument> packs)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"linguistics-content-{Guid.NewGuid():N}");
        foreach (var pack in packs)
        {
            var packDirectory = Path.Combine(directory, pack.Manifest.Id);
            Directory.CreateDirectory(packDirectory);
            File.WriteAllText(
                Path.Combine(packDirectory, "pack.json"),
                JsonSerializer.Serialize(pack, SerializerOptions));
        }

        return directory;
    }

    private static ValidatedContentCatalog LoadBundled(ContentLoadPolicy policy) =>
        ContentPackLoader.LoadDirectory(
            Path.Combine(AppContext.BaseDirectory, "Content"),
            policy);
}
