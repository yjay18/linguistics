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
        Assert.IsTrue(catalog.Packs.All(pack => pack.Manifest.SchemaVersion == 3));
        Assert.HasCount(1, german.Lessons);
        Assert.HasCount(3, german.Lessons[0].TemplateInstances);
        Assert.IsTrue(catalog.Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer)
            .All(pack => pack.Lessons.Count == 0));
        Assert.IsTrue(german.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
    }

    [TestMethod]
    public void CompleteTwoLanguageLearnerTextMapsValidate()
    {
        var pack = TwoLanguageTargetFixture();

        var errors = ContentPackValidator.Validate(
            [pack],
            ContentLoadPolicy.AuthoringPreview);

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        CollectionAssert.AreEqual(
            new[] { "en", "hi" },
            pack.Manifest.InstructionLanguages.ToArray());
        Assert.AreEqual("Hallo!", pack.Concepts[0].Examples[0].Text);
    }

    [TestMethod]
    public void DeclaredInstructionLanguageNeedsCoverageOnEveryLearnerField()
    {
        var pack = TwoLanguageTargetFixture();
        pack = ReplaceConcept(
            pack,
            0,
            pack.Concepts[0] with
            {
                Title = new Dictionary<string, string>
                {
                    ["en"] = pack.Concepts[0].Title["en"],
                },
            });

        var errors = ContentPackValidator.Validate(
            [pack],
            ContentLoadPolicy.AuthoringPreview);
        var error = errors.Single(candidate => candidate.Code == "instruction.coverage");

        Assert.AreEqual("language.de.core", error.PackId);
        Assert.AreEqual("concepts[0].title", error.Path);
        StringAssert.Contains(error.Message, "'hi'");
    }

    [TestMethod]
    public void PackNeedsAnInstructionLanguageDeclaration()
    {
        var pack = TwoLanguageTargetFixture();
        pack = pack with
        {
            Manifest = pack.Manifest with { InstructionLanguages = [] },
        };

        var errors = ContentPackValidator.Validate(
            [pack],
            ContentLoadPolicy.AuthoringPreview);
        var error = errors.Single(candidate =>
            candidate.Code == "language.missing" &&
            candidate.Path == "manifest.instructionLanguages");

        Assert.AreEqual("language.de.core", error.PackId);
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
        var bundled = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var approved = bundled.Packs.Select(ApproveForTestOnly)
            .ToArray();
        var directory = WritePacks(
            approved,
            bundled.Assets.Select(ApproveAssetForTestOnly).ToArray());
        try
        {
            var runtime = ContentPackLoader.LoadDirectory(directory, ContentLoadPolicy.Runtime);
            var graph = runtime.CreateRuntimeConceptGraph(
                new LanguageCode("de"),
                new LanguageCode("en"));
            var english = runtime.CreateRuntimeTransferMappings(
                new LanguageCode("en"),
                new LanguageCode("de"));
            var hindiNotes = runtime.CreateRuntimeTransferNotes(
                new LanguageCode("hi"),
                new LanguageCode("de"),
                new LanguageCode("en"));
            var cafe = runtime.CreateRuntimeCafeOrderDefinition(new LanguageCode("en"));
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
            catalog.CreateRuntimeConceptGraph(
                new LanguageCode("de"),
                new LanguageCode("en")));
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
            .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));
        var repeated = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));

        Assert.AreEqual(CoursePublicationState.Preview, catalog.PublicationState);
        Assert.AreEqual(450, catalog.TargetLessonCount);
        Assert.AreEqual(13, catalog.AuthoredLessonCount);
        Assert.AreEqual(437, catalog.RemainingLessonCount);
        Assert.AreEqual("Greet someone", catalog.Units[0].Lessons[0].Title);
        var lessons = catalog.Units.SelectMany(unit => unit.Lessons).ToArray();
        var provingLesson = lessons.Single(lesson => lesson.Id == "lesson.de.lexicon.cafe-items");
        Assert.HasCount(3, provingLesson.Slides);
        Assert.IsTrue(provingLesson.Slides.All(slide => slide.Kind == CourseSlideKind.Template));
        CollectionAssert.AreEqual(
            new[] { "object-spotlight", "picture-match", "word-order-train" },
            provingLesson.Slides
                .Select(slide => slide.TemplateInstance!.TemplateId.Value)
                .ToArray());
        Assert.IsTrue(lessons
            .Where(lesson => lesson != provingLesson)
            .All(lesson => lesson.Slides.Count >= 5));
        CollectionAssert.AreEqual(
            catalog.Units.SelectMany(unit => unit.Lessons).Select(lesson => lesson.Id).ToArray(),
            repeated.Units.SelectMany(unit => unit.Lessons).Select(lesson => lesson.Id).ToArray());
        CollectionAssert.AreEqual(
            catalog.Units.SelectMany(unit => unit.Lessons).SelectMany(lesson => lesson.Slides).Select(slide => slide.Id).ToArray(),
            repeated.Units.SelectMany(unit => unit.Lessons).SelectMany(lesson => lesson.Slides).Select(slide => slide.Id).ToArray());
        CollectionAssert.AreEqual(
            PresentationIds(catalog),
            PresentationIds(repeated));
    }

    [TestMethod]
    public void CourseProjectionIsExplicitAndDeterministicPerInstructionLanguage()
    {
        var directory = WritePacks([TwoLanguageTargetFixture()]);
        try
        {
            var catalog = ContentPackLoader.LoadDirectory(
                directory,
                ContentLoadPolicy.AuthoringPreview);
            var german = new LanguageCode("de");
            var english = catalog.CreateCourseCatalog(german, new LanguageCode("en"));
            var hindi = catalog.CreateCourseCatalog(german, new LanguageCode("hi"));
            var repeatedHindi = catalog.CreateCourseCatalog(german, new LanguageCode("hi"));

            CollectionAssert.AreEqual(
                new[] { new LanguageCode("en"), new LanguageCode("hi") },
                catalog.GetInstructionLanguages(german).ToArray());
            Assert.AreEqual(new LanguageCode("en"), english.InstructionLanguage);
            Assert.AreEqual(new LanguageCode("hi"), hindi.InstructionLanguage);
            Assert.AreEqual("Greet someone", english.Units[0].Lessons[0].Title);
            Assert.AreEqual("किसी का अभिवादन करें", hindi.Units[0].Lessons[0].Title);
            CollectionAssert.AreEqual(PresentationIds(english), PresentationIds(hindi));
            CollectionAssert.AreEqual(PresentationIds(hindi), PresentationIds(repeatedHindi));

            var englishExample = english.Units[0].Lessons[0].Slides
                .First(slide => slide.Kind == CourseSlideKind.Example);
            var hindiExample = hindi.Units[0].Lessons[0].Slides
                .First(slide => slide.Kind == CourseSlideKind.Example);
            Assert.AreEqual("Hallo!", englishExample.Title);
            Assert.AreEqual(englishExample.Title, hindiExample.Title);
            Assert.AreEqual("Hello!", englishExample.Body);
            Assert.AreEqual("नमस्ते", hindiExample.Body);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                catalog.CreateCourseCatalog(german, new LanguageCode("fr")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void AuthoredTemplatesReplaceFallbackInPackOrderAndResolveReferences()
    {
        var packs = LoadBundled(ContentLoadPolicy.AuthoringPreview).Packs.ToArray();
        var targetIndex = Array.FindIndex(packs, pack => pack.Manifest.Id == "language.de.core");
        var target = packs[targetIndex];
        var concept = target.Concepts[0] with
        {
            Examples = Replace(
                target.Concepts[0].Examples,
                0,
                target.Concepts[0].Examples[0] with { Id = "de.example.catalog-fixture" }),
        };
        target = ReplaceConcept(target, 0, concept);
        var lessonId = $"lesson.{concept.Id}";
        target = target with
        {
            Lessons =
            [
                new LessonTemplateContent(
                    lessonId,
                    [
                        ObjectSpotlightInstance(lessonId, 1, concept, "Hallo"),
                        ObjectSpotlightInstance(lessonId, 2, concept, "Guten Tag"),
                    ]),
            ],
        };
        packs[targetIndex] = target;
        var directory = WritePacks(packs);
        try
        {
            var first = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview)
                .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));
            var repeated = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview)
                .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));
            var authored = first.Units
                .SelectMany(unit => unit.Lessons)
                .Single(lesson => lesson.Id == lessonId);

            Assert.HasCount(2, authored.Slides);
            Assert.IsTrue(authored.Slides.All(slide => slide.Kind == CourseSlideKind.Template));
            CollectionAssert.AreEqual(
                new[]
                {
                    $"{lessonId}.template.01",
                    $"{lessonId}.template.02",
                },
                authored.Slides.Select(slide => slide.TemplateInstance!.Id).ToArray());
            Assert.AreEqual(
                concept.Id,
                authored.Slides[0].TemplateInstance!.Parameters.Values["concept"].Concept!.Id);
            Assert.AreEqual(
                "de.example.catalog-fixture",
                authored.Slides[0].TemplateInstance!.Parameters.Values["example"].Example!.Id);
            Assert.AreEqual(
                "नमस्ते देखें।",
                authored.Slides[0].TemplateInstance!.Parameters.Values["instruction"].TextByLanguage!["hi"]);

            var fallback = first.Units
                .SelectMany(unit => unit.Lessons)
                .First(lesson => lesson.Id != lessonId);
            Assert.IsGreaterThanOrEqualTo(5, fallback.Slides.Count);
            Assert.IsTrue(fallback.Slides.All(slide => slide.TemplateInstance is null));

            CollectionAssert.AreEqual(
                PresentationIds(first),
                PresentationIds(repeated));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void RuntimeApprovedContentBecomesReadyLessons()
    {
        var bundled = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var approved = bundled.Packs.Select(ApproveForTestOnly)
            .ToArray();
        var directory = WritePacks(
            approved,
            bundled.Assets.Select(ApproveAssetForTestOnly).ToArray());
        try
        {
            var course = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.Runtime)
                .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));

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
                .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));

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
    [DataRow("missing-lessons", "lesson.collection")]
    [DataRow("missing-lesson", "lesson.missing")]
    [DataRow("broken-lesson-binding", "lesson.reference")]
    [DataRow("duplicate-lesson-id", "id.duplicate")]
    [DataRow("empty-template-instances", "template.collection")]
    [DataRow("missing-template-instance", "template.instance")]
    [DataRow("invalid-template-version", "template.version")]
    [DataRow("missing-template-parameters", "template.parameters")]
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
                        english.TransferMappings[0] with
                        {
                            LearnerExplanation = new Dictionary<string, string>(),
                        }),
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
            case "missing-lessons":
                target = target with { Lessons = null! };
                break;
            case "missing-lesson":
                target = target with { Lessons = [null!] };
                break;
            case "broken-lesson-binding":
                target = target with
                {
                    Lessons = [LessonFixture(target.Concepts[0]) with { Id = "lesson.de.missing" }],
                };
                break;
            case "duplicate-lesson-id":
                var duplicatedLesson = LessonFixture(target.Concepts[0]);
                target = target with { Lessons = [duplicatedLesson, duplicatedLesson] };
                break;
            case "empty-template-instances":
                target = target with
                {
                    Lessons = [LessonFixture(target.Concepts[0]) with { TemplateInstances = [] }],
                };
                break;
            case "missing-template-instance":
                target = target with
                {
                    Lessons = [LessonFixture(target.Concepts[0]) with { TemplateInstances = [null!] }],
                };
                break;
            case "invalid-template-version":
                var invalidVersionLesson = LessonFixture(target.Concepts[0]);
                target = target with
                {
                    Lessons =
                    [
                        invalidVersionLesson with
                        {
                            TemplateInstances =
                            [
                                invalidVersionLesson.TemplateInstances[0] with { TemplateVersion = 0 },
                            ],
                        },
                    ],
                };
                break;
            case "missing-template-parameters":
                var missingParametersLesson = LessonFixture(target.Concepts[0]);
                target = target with
                {
                    Lessons =
                    [
                        missingParametersLesson with
                        {
                            TemplateInstances =
                            [
                                missingParametersLesson.TemplateInstances[0] with { Parameters = null! },
                            ],
                        },
                    ],
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

    private static LessonTemplateContent LessonFixture(TargetConceptContent concept)
    {
        var lessonId = $"lesson.{concept.Id}";
        return new LessonTemplateContent(
            lessonId,
            [
                new TemplateInstance(
                    $"{lessonId}.template.01",
                    new TemplateId("fixture-template"),
                    1,
                    new Dictionary<string, TemplateParameterValue>()),
            ]);
    }

    private static TemplateInstance ObjectSpotlightInstance(
        string lessonId,
        int number,
        TargetConceptContent concept,
        string word) =>
        new(
            $"{lessonId}.template.{number:00}",
            new TemplateId("object-spotlight"),
            1,
            new Dictionary<string, TemplateParameterValue>
            {
                ["word"] = new(TemplateParameterKind.Text, Value: word),
                ["meaning"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "greeting",
                        ["hi"] = "अभिवादन",
                    }),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Notice this greeting.",
                        ["hi"] = "नमस्ते देखें।",
                    }),
                ["concept"] = new(TemplateParameterKind.ConceptReference, Value: concept.Id),
                ["example"] = new(
                    TemplateParameterKind.ExampleReference,
                    Value: "de.example.catalog-fixture"),
            });

    private static string[] PresentationIds(CourseCatalog course) =>
        course.Units
            .SelectMany(unit => unit.Lessons)
            .SelectMany(lesson => lesson.Slides.Select(slide => string.Join(
                '|',
                lesson.Id,
                slide.Id,
                slide.TemplateInstance?.Id ?? "fallback",
                slide.TemplateInstance?.TemplateId.Value ?? "fallback")))
            .ToArray();

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

    private static ValidatedContentAsset ApproveAssetForTestOnly(ValidatedContentAsset asset)
    {
        var review = new ContentReview(
            ContentReviewStatus.Approved,
            "Synthetic test reviewer",
            new DateOnly(2026, 8, 30),
            "Test-only asset approval proving the runtime gate; not approval of bundled media.");
        return asset with
        {
            Record = asset.Record with
            {
                License = asset.Record.License with
                {
                    ModificationReviewed = true,
                    RedistributionReviewed = true,
                    ReviewStatus = LicenseReviewStatus.Reviewed,
                },
                Transformation = asset.Record.Transformation with
                {
                    QaStatus = ContentAssetQaStatus.HumanReviewed,
                    QaNotes = "Synthetic test-only human review marker.",
                },
                Review = review,
            },
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
                Title = new Dictionary<string, string>
                {
                    ["en"] = $"Synthetic lesson {index}",
                },
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
            Lessons = [],
        };
    }

    private static ContentPackDocument TwoLanguageTargetFixture()
    {
        var source = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var concept = source.Concepts[0];
        concept = concept with
        {
            Title = AddHindi(concept.Title, "किसी का अभिवादन करें"),
            Description = AddHindi(
                concept.Description,
                "Hallo या Guten Tag से रोज़मर्रा की छोटी बातचीत शुरू करें।"),
            Examples = concept.Examples
                .Select(example => example with
                {
                    Meaning = AddHindi(example.Meaning, "नमस्ते"),
                    Note = AddHindi(example.Note, "सामान्य अभिवादन।"),
                })
                .ToArray(),
            Counterexamples = concept.Counterexamples
                .Select(example => example with
                {
                    Meaning = AddHindi(example.Meaning, "उदाहरण"),
                    Note = AddHindi(example.Note, "स्पष्टीकरण।"),
                })
                .ToArray(),
        };

        return source with
        {
            Manifest = source.Manifest with { InstructionLanguages = ["en", "hi"] },
            Concepts = [concept],
            Lexicon = [],
            Tasks = [],
            ErrorRules = [],
            FeedbackTemplates = [],
            Rubrics = [],
            PronunciationUtterances = [],
            Lessons = [],
        };
    }

    private static IReadOnlyDictionary<string, string> AddHindi(
        IReadOnlyDictionary<string, string> values,
        string hindi) =>
        values
            .Append(new KeyValuePair<string, string>("hi", hindi))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static string WritePacks(
        IEnumerable<ContentPackDocument> packs,
        IReadOnlyList<ValidatedContentAsset>? assets = null)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"linguistics-content-{Guid.NewGuid():N}");
        foreach (var pack in packs)
        {
            var packDirectory = Path.Combine(directory, pack.Manifest.Id);
            Directory.CreateDirectory(packDirectory);
            File.WriteAllText(
                Path.Combine(packDirectory, "pack.json"),
                JsonSerializer.Serialize(pack, SerializerOptions));
            var packAssets = assets?
                .Where(asset => asset.PackId == pack.Manifest.Id)
                .OrderBy(asset => asset.Record.Id, StringComparer.Ordinal)
                .ToArray() ?? [];
            if (packAssets.Length == 0)
            {
                continue;
            }

            foreach (var asset in packAssets)
            {
                var destination = Path.Combine(
                    packDirectory,
                    asset.Record.File.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(asset.AbsoluteFilePath, destination);
            }

            var manifest = new ContentAssetManifest(
                1,
                pack.Manifest.Id,
                pack.Manifest.Version,
                packAssets.Select(asset => asset.Record).ToArray());
            File.WriteAllText(
                Path.Combine(packDirectory, "assets.json"),
                JsonSerializer.Serialize(manifest, SerializerOptions));
        }

        return directory;
    }

    private static ValidatedContentCatalog LoadBundled(ContentLoadPolicy policy) =>
        ContentPackLoader.LoadDirectory(
            Path.Combine(AppContext.BaseDirectory, "Content"),
            policy);
}
