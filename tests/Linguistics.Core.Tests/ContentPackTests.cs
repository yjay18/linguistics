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
        Assert.HasCount(14, catalog.Packs);
        var german = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var unitOne = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit01");
        var unitTwo = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit02");
        var unitThree = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit03");
        var unitFour = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit04");
        var unitFive = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit05");
        var unitSix = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit06");
        var unitSeven = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit07");
        var unitEight = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit08");
        var unitNine = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit09");
        var unitTen = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a2.unit10");
        var unitEleven = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a2.unit11");
        Assert.HasCount(13, german.Concepts);
        Assert.HasCount(10, unitOne.Concepts);
        Assert.HasCount(10, unitTwo.Concepts);
        Assert.HasCount(10, unitThree.Concepts);
        Assert.HasCount(10, unitFour.Concepts);
        Assert.HasCount(10, unitFive.Concepts);
        Assert.HasCount(10, unitSix.Concepts);
        Assert.HasCount(10, unitSeven.Concepts);
        Assert.HasCount(10, unitEight.Concepts);
        Assert.HasCount(10, unitNine.Concepts);
        Assert.HasCount(10, unitTen.Concepts);
        Assert.HasCount(10, unitEleven.Concepts);
        Assert.HasCount(26, unitOne.Lexicon);
        Assert.HasCount(26, unitTwo.Lexicon);
        Assert.HasCount(26, unitThree.Lexicon);
        Assert.HasCount(32, unitFour.Lexicon);
        Assert.HasCount(32, unitFive.Lexicon);
        Assert.HasCount(32, unitSix.Lexicon);
        Assert.HasCount(32, unitSeven.Lexicon);
        Assert.HasCount(32, unitEight.Lexicon);
        Assert.HasCount(32, unitNine.Lexicon);
        Assert.HasCount(32, unitTen.Lexicon);
        Assert.HasCount(32, unitEleven.Lexicon);
        Assert.HasCount(4, german.Tasks);
        Assert.HasCount(1, unitOne.Tasks);
        Assert.HasCount(1, unitTwo.Tasks);
        Assert.HasCount(1, unitThree.Tasks);
        Assert.HasCount(1, unitFour.Tasks);
        Assert.HasCount(1, unitFive.Tasks);
        Assert.HasCount(1, unitSix.Tasks);
        Assert.HasCount(1, unitSeven.Tasks);
        Assert.HasCount(1, unitEight.Tasks);
        Assert.HasCount(1, unitNine.Tasks);
        Assert.HasCount(1, unitTen.Tasks);
        Assert.HasCount(1, unitEleven.Tasks);
        Assert.HasCount(5, german.ErrorRules);
        Assert.HasCount(10, unitOne.ErrorRules);
        Assert.HasCount(10, unitTwo.ErrorRules);
        Assert.HasCount(10, unitThree.ErrorRules);
        Assert.HasCount(10, unitFour.ErrorRules);
        Assert.HasCount(10, unitFive.ErrorRules);
        Assert.HasCount(10, unitSix.ErrorRules);
        Assert.HasCount(10, unitSeven.ErrorRules);
        Assert.HasCount(10, unitEight.ErrorRules);
        Assert.HasCount(10, unitNine.ErrorRules);
        Assert.HasCount(10, unitTen.ErrorRules);
        Assert.HasCount(10, unitEleven.ErrorRules);
        Assert.HasCount(10, unitOne.FeedbackTemplates);
        Assert.HasCount(10, unitTwo.FeedbackTemplates);
        Assert.HasCount(10, unitThree.FeedbackTemplates);
        Assert.HasCount(10, unitFour.FeedbackTemplates);
        Assert.HasCount(10, unitFive.FeedbackTemplates);
        Assert.HasCount(10, unitSix.FeedbackTemplates);
        Assert.HasCount(10, unitSeven.FeedbackTemplates);
        Assert.HasCount(10, unitEight.FeedbackTemplates);
        Assert.HasCount(10, unitNine.FeedbackTemplates);
        Assert.HasCount(10, unitTen.FeedbackTemplates);
        Assert.HasCount(10, unitEleven.FeedbackTemplates);
        Assert.HasCount(4, german.Rubrics);
        Assert.HasCount(1, unitOne.Rubrics);
        Assert.HasCount(1, unitTwo.Rubrics);
        Assert.HasCount(1, unitThree.Rubrics);
        Assert.HasCount(1, unitFour.Rubrics);
        Assert.HasCount(1, unitFive.Rubrics);
        Assert.HasCount(1, unitSix.Rubrics);
        Assert.HasCount(1, unitSeven.Rubrics);
        Assert.HasCount(1, unitEight.Rubrics);
        Assert.HasCount(1, unitNine.Rubrics);
        Assert.HasCount(1, unitTen.Rubrics);
        Assert.HasCount(1, unitEleven.Rubrics);
        Assert.HasCount(4, german.PronunciationUtterances);
        Assert.HasCount(10, unitOne.PronunciationUtterances);
        Assert.HasCount(10, unitTwo.PronunciationUtterances);
        Assert.HasCount(10, unitThree.PronunciationUtterances);
        Assert.HasCount(10, unitFour.PronunciationUtterances);
        Assert.HasCount(10, unitFive.PronunciationUtterances);
        Assert.HasCount(10, unitSix.PronunciationUtterances);
        Assert.HasCount(10, unitSeven.PronunciationUtterances);
        Assert.HasCount(10, unitEight.PronunciationUtterances);
        Assert.HasCount(10, unitNine.PronunciationUtterances);
        Assert.HasCount(10, unitTen.PronunciationUtterances);
        Assert.HasCount(10, unitEleven.PronunciationUtterances);
        Assert.AreEqual(4, german.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitOne.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitTwo.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitThree.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitFour.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitFive.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitSix.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitSeven.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitEight.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitNine.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitTen.Manifest.SchemaVersion);
        Assert.AreEqual(4, unitEleven.Manifest.SchemaVersion);
        Assert.IsTrue(catalog.Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer)
            .All(pack => pack.Manifest.SchemaVersion == 3));
        Assert.IsEmpty(german.Lessons);
        Assert.HasCount(10, unitOne.Lessons);
        Assert.AreEqual(79, unitOne.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitTwo.Lessons);
        Assert.AreEqual(80, unitTwo.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitThree.Lessons);
        Assert.AreEqual(80, unitThree.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitFour.Lessons);
        Assert.AreEqual(80, unitFour.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitFive.Lessons);
        Assert.AreEqual(80, unitFive.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitSix.Lessons);
        Assert.AreEqual(80, unitSix.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitSeven.Lessons);
        Assert.AreEqual(80, unitSeven.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitEight.Lessons);
        Assert.AreEqual(80, unitEight.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitNine.Lessons);
        Assert.AreEqual(80, unitNine.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitTen.Lessons);
        Assert.AreEqual(80, unitTen.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(10, unitEleven.Lessons);
        Assert.AreEqual(80, unitEleven.Lessons.Sum(lesson => lesson.TemplateInstances.Count));
        Assert.HasCount(1, unitOne.CourseUnits!);
        Assert.HasCount(1, unitTwo.CourseUnits!);
        Assert.HasCount(1, unitThree.CourseUnits!);
        Assert.HasCount(1, unitFour.CourseUnits!);
        Assert.HasCount(1, unitFive.CourseUnits!);
        Assert.HasCount(1, unitSix.CourseUnits!);
        Assert.HasCount(1, unitSeven.CourseUnits!);
        Assert.HasCount(1, unitEight.CourseUnits!);
        Assert.HasCount(1, unitNine.CourseUnits!);
        Assert.HasCount(1, unitTen.CourseUnits!);
        Assert.HasCount(1, unitEleven.CourseUnits!);
        Assert.IsTrue(catalog.Packs
            .Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer)
            .All(pack => pack.Lessons.Count == 0));
        Assert.IsTrue(german.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitOne.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitTwo.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitThree.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitFour.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitFive.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitSix.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitSeven.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitEight.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitNine.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitTen.PronunciationUtterances.All(utterance =>
            utterance.AssessmentMode == PronunciationAssessmentMode.None));
        Assert.IsTrue(unitEleven.PronunciationUtterances.All(utterance =>
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
        var targets = catalog.Packs.Where(pack => pack.Manifest.Kind == ContentPackKind.TargetLanguage).ToArray();
        var german = targets.Single(pack => pack.Manifest.Id == "language.de.core");
        var transfers = catalog.Packs.Where(pack => pack.Manifest.Kind == ContentPackKind.Transfer).ToArray();

        Assert.HasCount(12, targets);
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
    public void FifteenTasksHaveReachableDeterministicSuccessContractsAndFallbacks()
    {
        var tasks = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.SelectMany(pack => pack.Tasks)
            .ToArray();

        Assert.HasCount(15, tasks);
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
                new LanguageCode("hi"));
            var hinglishNotes = runtime.CreateRuntimeTransferNotes(
                new LanguageCode("hi"),
                new LanguageCode("de"),
                new LanguageCode("hi-latn"));
            var cafe = runtime.CreateRuntimeCafeOrderDefinition(new LanguageCode("en"));
            var pronunciation = runtime.CreateRuntimePronunciationUtterances(
                new LanguageCode("de"));

            Assert.HasCount(123, graph.Nodes);
            Assert.HasCount(3, english);
            Assert.IsTrue(english.All(mapping => mapping.ReviewStatus == TransferReviewStatus.Approved));
            Assert.IsTrue(hindiNotes.Any(note =>
                note.Mapping.TargetConceptId == new ConceptId("de.noun.gender-basic")));
            Assert.IsTrue(hindiNotes.All(note => note.LearnerExplanation.Any(character =>
                character is >= '\u0900' and <= '\u097f')));
            Assert.IsTrue(hindiNotes.SelectMany(note => note.NegativeTransferRisks).All(risk =>
                risk.Any(character => character is >= '\u0900' and <= '\u097f')));
            Assert.HasCount(2, hinglishNotes);
            Assert.IsTrue(hinglishNotes.All(note =>
                note.LearnerExplanation.All(character => character is < '\u0900' or > '\u097f')));
            Assert.IsTrue(hinglishNotes.SelectMany(note => note.NegativeTransferRisks).All(risk =>
                risk.All(character => character is < '\u0900' or > '\u097f')));
            Assert.AreEqual("de.task.cafe.order-one-item", cafe.TaskId);
            Assert.AreEqual(new ConceptId("de.function.order-polite"), cafe.TargetConceptId);
            Assert.IsNotEmpty(cafe.ScriptedResponses[cafe.CompleteStateId]);
            Assert.AreEqual("Ich möchte einen Kaffee, bitte.", cafe.PronunciationTargetText);
            Assert.HasCount(114, pronunciation);
            Assert.HasCount(4, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.core.v2")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit01.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit02.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit03.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit04.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit05.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit06.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit07.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit08.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a1.unit09.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a2.unit10.v1")));
            Assert.HasCount(10, pronunciation.Where(utterance =>
                utterance.ContentVersion == new VersionId("language.de.a2.unit11.v1")));
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
        Assert.AreEqual(110, catalog.AuthoredLessonCount);
        Assert.AreEqual(340, catalog.RemainingLessonCount);
        Assert.AreEqual("Meet and greet", catalog.Units[0].Title);
        Assert.AreEqual("Greet for the time of day", catalog.Units[0].Lessons[0].Title);
        Assert.AreEqual("Learn in German", catalog.Units[1].Title);
        Assert.AreEqual("Recognize classroom objects", catalog.Units[1].Lessons[0].Title);
        Assert.AreEqual("Numbers, dates, and time", catalog.Units[2].Title);
        Assert.AreEqual("Count and group objects", catalog.Units[2].Lessons[0].Title);
        Assert.AreEqual("People and family", catalog.Units[3].Title);
        Assert.AreEqual("Name family members", catalog.Units[3].Lessons[0].Title);
        Assert.AreEqual("Daily routines", catalog.Units[4].Title);
        Assert.AreEqual("Name daily actions", catalog.Units[4].Lessons[0].Title);
        Assert.AreEqual("Food and café visits", catalog.Units[5].Title);
        Assert.AreEqual("Recognize café items", catalog.Units[5].Lessons[0].Title);
        Assert.AreEqual("Home and belongings", catalog.Units[6].Title);
        Assert.AreEqual("Name rooms", catalog.Units[6].Lessons[0].Title);
        Assert.AreEqual("Around town", catalog.Units[7].Title);
        Assert.AreEqual("Name places in town", catalog.Units[7].Lessons[0].Title);
        Assert.AreEqual("A1 independence", catalog.Units[8].Title);
        Assert.AreEqual("Check personal information", catalog.Units[8].Lessons[0].Title);
        Assert.AreEqual("Recent experiences", catalog.Units[9].Title);
        Assert.AreEqual("Recognize completed actions", catalog.Units[9].Lessons[0].Title);
        Assert.AreEqual("Health and appointments", catalog.Units[10].Title);
        Assert.AreEqual("Name body areas", catalog.Units[10].Lessons[0].Title);
        var lessons = catalog.Units.SelectMany(unit => unit.Lessons).ToArray();
        CollectionAssert.AreEqual(
            new[]
            {
                "lesson.de.a1.u01.greetings-by-time",
                "lesson.de.a1.u01.say-name",
                "lesson.de.a1.u01.ask-name-informal",
                "lesson.de.a1.u01.ask-name-formal",
                "lesson.de.a1.u01.say-origin",
                "lesson.de.a1.u01.say-languages",
                "lesson.de.a1.u01.hear-introductions",
                "lesson.de.a1.u01.spell-name",
                "lesson.de.a1.u01.introduce-two-people",
                "lesson.de.a1.u01.first-meeting-mission",
                "lesson.de.a1.u02.classroom-objects",
                "lesson.de.a1.u02.ask-object-name",
                "lesson.de.a1.u02.say-have-object",
                "lesson.de.a1.u02.follow-instructions",
                "lesson.de.a1.u02.request-repetition",
                "lesson.de.a1.u02.state-understanding",
                "lesson.de.a1.u02.hear-letter-names",
                "lesson.de.a1.u02.read-labels",
                "lesson.de.a1.u02.mediate-instruction",
                "lesson.de.a1.u02.classroom-mission",
                "lesson.de.a1.u03.count-through-twenty",
                "lesson.de.a1.u03.build-larger-numbers",
                "lesson.de.a1.u03.share-phone-number",
                "lesson.de.a1.u03.tell-time",
                "lesson.de.a1.u03.name-days-dates",
                "lesson.de.a1.u03.say-birthday",
                "lesson.de.a1.u03.hear-prices-times",
                "lesson.de.a1.u03.read-opening-hours",
                "lesson.de.a1.u03.confirm-appointment",
                "lesson.de.a1.u03.schedule-mission",
                "lesson.de.a1.u04.family-members",
                "lesson.de.a1.u04.who-someone-is",
                "lesson.de.a1.u04.give-age",
                "lesson.de.a1.u04.describe-appearance",
                "lesson.de.a1.u04.describe-character",
                "lesson.de.a1.u04.use-mein-dein",
                "lesson.de.a1.u04.hear-family-details",
                "lesson.de.a1.u04.read-short-profile",
                "lesson.de.a1.u04.introduce-family-member",
                "lesson.de.a1.u04.people-mission",
                "lesson.de.a1.u05.daily-actions",
                "lesson.de.a1.u05.regular-present",
                "lesson.de.a1.u05.irregular-present",
                "lesson.de.a1.u05.separable-verbs",
                "lesson.de.a1.u05.sequence-morning",
                "lesson.de.a1.u05.like-doing",
                "lesson.de.a1.u05.hear-daily-schedule",
                "lesson.de.a1.u05.read-simple-calendar",
                "lesson.de.a1.u05.compare-routines",
                "lesson.de.a1.u05.routine-mission",
                "lesson.de.a1.u06.cafe-items",
                "lesson.de.a1.u06.say-would-like",
                "lesson.de.a1.u06.order-politely",
                "lesson.de.a1.u06.accusative-articles",
                "lesson.de.a1.u06.ask-availability",
                "lesson.de.a1.u06.say-do-not-want",
                "lesson.de.a1.u06.hear-cafe-order",
                "lesson.de.a1.u06.read-simple-menu",
                "lesson.de.a1.u06.mediate-group-order",
                "lesson.de.a1.u06.cafe-mission",
                "lesson.de.a1.u07.rooms",
                "lesson.de.a1.u07.furniture-objects",
                "lesson.de.a1.u07.es-gibt",
                "lesson.de.a1.u07.locate-object",
                "lesson.de.a1.u07.ask-where",
                "lesson.de.a1.u07.describe-size-quality",
                "lesson.de.a1.u07.hear-room-description",
                "lesson.de.a1.u07.read-rental-notice",
                "lesson.de.a1.u07.mediate-home-layout",
                "lesson.de.a1.u07.home-mission",
                "lesson.de.a1.u08.town-places",
                "lesson.de.a1.u08.ask-place",
                "lesson.de.a1.u08.direction-words",
                "lesson.de.a1.u08.give-directions",
                "lesson.de.a1.u08.travel-with-mit",
                "lesson.de.a1.u08.ask-destination",
                "lesson.de.a1.u08.hear-station-announcement",
                "lesson.de.a1.u08.read-route-display",
                "lesson.de.a1.u08.mediate-visitor-route",
                "lesson.de.a1.u08.town-mission",
                "lesson.de.a1.u09.check-personal-information",
                "lesson.de.a1.u09.check-time-quantity",
                "lesson.de.a1.u09.check-everyday-actions",
                "lesson.de.a1.u09.check-people-things",
                "lesson.de.a1.u09.read-public-message",
                "lesson.de.a1.u09.write-brief-message",
                "lesson.de.a1.u09.hear-everyday-exchanges",
                "lesson.de.a1.u09.repair-conversation",
                "lesson.de.a1.u09.mediate-a1-plan",
                "lesson.de.a1.u09.a1-capstone",
                "lesson.de.a2.u10.recognize-completed-actions",
                "lesson.de.a2.u10.build-regular-participles",
                "lesson.de.a2.u10.use-irregular-participles",
                "lesson.de.a2.u10.choose-haben-or-sein",
                "lesson.de.a2.u10.sequence-yesterday",
                "lesson.de.a2.u10.ask-follow-up-questions",
                "lesson.de.a2.u10.hear-weekend-account",
                "lesson.de.a2.u10.read-personal-update",
                "lesson.de.a2.u10.retell-experience",
                "lesson.de.a2.u10.weekend-mission",
                "lesson.de.a2.u11.name-body-areas",
                "lesson.de.a2.u11.describe-symptom",
                "lesson.de.a2.u11.say-how-long",
                "lesson.de.a2.u11.request-appointment",
                "lesson.de.a2.u11.understand-advice",
                "lesson.de.a2.u11.follow-medicine-instructions",
                "lesson.de.a2.u11.hear-reception-call",
                "lesson.de.a2.u11.read-pharmacy-label",
                "lesson.de.a2.u11.relay-symptom",
                "lesson.de.a2.u11.health-mission",
            },
            lessons.Select(lesson => lesson.Id).ToArray());
        Assert.IsTrue(lessons.All(lesson => lesson.Slides.Count >= 7));
        Assert.IsTrue(lessons.All(lesson =>
            lesson.Slides[0].TemplateInstance!.TemplateId == new TemplateId("scene-establish")));
        Assert.IsTrue(lessons.All(lesson =>
            lesson.Slides[^1].TemplateInstance!.TemplateId == new TemplateId("recap-scrapbook")));
        Assert.IsTrue(lessons.SelectMany(lesson => lesson.Slides).All(slide =>
            slide.Kind == CourseSlideKind.Template));
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
    public void UnitOneActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit01");
        var greeting = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("picture-match"));
        var name = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-type"));
        var origin = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("note-write"));
        var languages = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var spelling = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("spelling-tiles"));
        var introduction = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("picture-match"));
        var meeting = unit.Lessons[9];
        var scenario = meeting.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var capstone = meeting.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var greetingOptions = greeting.Parameters["options"].Options!;
        var greetingAnswer = greeting.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator
                .EvaluatePictureMatch(greetingOptions, greetingAnswer, "evening")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator
                .EvaluatePictureMatch(greetingOptions, greetingAnswer, "morning")
                .State);

        var acceptedNames = name.Parameters["accepted-answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                acceptedNames,
                "Ich heiße Mina.")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(acceptedNames, " ")
                .State);

        var requiredOrigin = origin.Parameters["required-content"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredOrigin,
                "Ich komme aus Indien.")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredOrigin,
                "Indien")
                .State);

        var languageOptions = languages.Parameters["options"].Options!;
        var languageOrder = languageOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(languageOptions, languageOrder).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                languageOptions,
                languageOrder.Reverse().ToArray())
                .State);

        var letterOptions = spelling.Parameters["letters"].Options!;
        var letterOrder = letterOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(letterOptions, letterOrder).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                letterOptions,
                [letterOrder[1], letterOrder[0], letterOrder[2], letterOrder[3]])
                .State);

        var introductionOptions = introduction.Parameters["options"].Options!;
        var introductionAnswer = introduction.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                introductionOptions,
                introductionAnswer,
                "introduce-omar")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                introductionOptions,
                introductionAnswer,
                "self-introduction")
                .State);

        var responses = scenario.Parameters["responses"].Options!;
        var responseAnswer = scenario.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                responses,
                responseAnswer,
                "greeting-name")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                responses,
                responseAnswer,
                "name-only")
                .State);

        var steps = capstone.Parameters["steps"].Options!;
        var chain = capstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                [],
                "greeting")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["greeting"],
                "detail")
                .State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["greeting", "name", "detail"],
                "farewell")
                .State);
    }

    [TestMethod]
    public void UnitTwoActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit02");
        var objects = unit.Lessons[0];
        var article = objects.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("article-stamp"));
        var sort = objects.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sort-into-baskets"));
        var questionOrder = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var articleGap = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var instructionOrder = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var repetition = unit.Lessons[4];
        var repetitionTyping = repetition.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-type"));
        var repetitionScenario = repetition.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var understanding = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("negation-strike"));
        var letterChoice = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("minimal-pair-doors"));
        var signChoice = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var mediationChoice = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("dialogue-eavesdrop"));
        var mission = unit.Lessons[9];
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var articleOptions = article.Parameters["options"].Options!;
        var articleAnswer = article.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                articleOptions,
                articleAnswer,
                "das").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                articleOptions,
                articleAnswer,
                "der").State);

        var items = sort.Parameters["items"].Options!;
        var baskets = sort.Parameters["baskets"].Options!;
        var expectedAssignments = sort.Parameters["answers"].Options!
            .ToDictionary(option => option.Id, option => option.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                items,
                baskets,
                expectedAssignments,
                expectedAssignments).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                items,
                baskets,
                expectedAssignments,
                new Dictionary<string, string>(expectedAssignments, StringComparer.Ordinal)
                {
                    ["buch"] = "der",
                }).State);

        var questionOptions = questionOrder.Parameters["options"].Options!;
        var questionIds = questionOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(questionOptions, questionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                questionOptions,
                questionIds.Reverse().ToArray()).State);

        var gapOptions = articleGap.Parameters["options"].Options!;
        var gapAnswer = articleGap.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                gapOptions,
                gapAnswer,
                "einen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                gapOptions,
                gapAnswer,
                "eine").State);

        var instructionEvents = instructionOrder.Parameters["events"].Options!;
        var instructionIds = instructionEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                instructionEvents,
                instructionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                instructionEvents,
                [instructionIds[1], instructionIds[0], instructionIds[2], instructionIds[3]])
                .State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                repetitionTyping.Parameters["accepted-answers"].Options!,
                "Noch einmal, bitte.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                repetitionTyping.Parameters["accepted-answers"].Options!,
                " ").State);

        var responses = repetitionScenario.Parameters["responses"].Options!;
        var responseAnswer = repetitionScenario.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                responses,
                responseAnswer,
                "repeat").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                responses,
                responseAnswer,
                "thanks").State);

        var negators = understanding.Parameters["negators"].Options!;
        var slots = understanding.Parameters["slots"].Options!;
        var expectedNegator = understanding.Parameters["answer-negator"].Value!;
        var expectedSlot = understanding.Parameters["answer-slot"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                expectedNegator,
                expectedSlot,
                "nicht",
                "after-object").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negators,
                slots,
                expectedNegator,
                expectedSlot,
                "kein",
                "before-object").State);

        var letterOptions = letterChoice.Parameters["options"].Options!;
        var letterAnswer = letterChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                letterOptions,
                letterAnswer,
                "letter-b").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                letterOptions,
                letterAnswer,
                "letter-p").State);

        var signOptions = signChoice.Parameters["options"].Options!;
        var signAnswer = signChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                signOptions,
                signAnswer,
                "write").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                signOptions,
                signAnswer,
                "read").State);

        var mediationOptions = mediationChoice.Parameters["options"].Options!;
        var mediationAnswer = mediationChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                mediationOptions,
                mediationAnswer,
                "write").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                mediationOptions,
                mediationAnswer,
                "listen").State);

        var missionResponses = missionScenario.Parameters["responses"].Options!;
        var missionAnswer = missionScenario.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionResponses,
                missionAnswer,
                "repeat").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionResponses,
                missionAnswer,
                "item").State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "identify").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["identify"],
                "clarify").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["identify", "request", "clarify"],
                "confirm").State);
    }

    [TestMethod]
    public void UnitThreeActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit03");
        var counting = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("number-tiles"));
        var largerNumber = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-type"));
        var telephone = unit.Lessons[2];
        var digitOrder = telephone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var telephoneForm = telephone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var timeChoice = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("picture-match"));
        var dateLesson = unit.Lessons[4];
        var dateChoice = dateLesson.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("schedule-read"));
        var dateNote = dateLesson.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("note-write"));
        var birthdayOrder = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var priceChoice = unit.Lessons[6].TemplateInstances
            .First(instance => instance.TemplateId == new TemplateId("listen-price-tag"));
        var openingChoice = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("schedule-read"));
        var appointmentChoice = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("dialogue-eavesdrop"));
        var mission = unit.Lessons[9];
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var countingOptions = counting.Parameters["options"].Options!;
        var countingAnswer = counting.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                countingOptions,
                countingAnswer,
                "12").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                countingOptions,
                countingAnswer,
                "10").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                largerNumber.Parameters["accepted-answers"].Options!,
                "zweiunddreißig").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                largerNumber.Parameters["accepted-answers"].Options!,
                " ").State);

        var digitEvents = digitOrder.Parameters["events"].Options!;
        var digitIds = digitEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(digitEvents, digitIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                digitEvents,
                [digitIds[1], digitIds[0], digitIds[2], digitIds[3]]).State);

        var telephoneAnswers = telephoneForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                telephoneAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["prefix"] = "0176",
                    ["number"] = "4298",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                telephoneAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["prefix"] = "0716",
                    ["number"] = "4298",
                }).State);

        var timeOptions = timeChoice.Parameters["options"].Options!;
        var timeAnswer = timeChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                timeOptions,
                timeAnswer,
                "nine").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                timeOptions,
                timeAnswer,
                "ten").State);

        var dateOptions = dateChoice.Parameters["options"].Options!;
        var dateAnswer = dateChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                dateOptions,
                dateAnswer,
                "may-three").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                dateOptions,
                dateAnswer,
                "may-two").State);

        var requiredDate = dateNote.Parameters["required-content"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredDate,
                "Termin: Dienstag, 3. Mai.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredDate,
                "Termin: Dienstag.").State);

        var birthdayOptions = birthdayOrder.Parameters["options"].Options!;
        var birthdayIds = birthdayOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                birthdayOptions,
                birthdayIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                birthdayOptions,
                birthdayIds.Reverse().ToArray()).State);

        var priceOptions = priceChoice.Parameters["options"].Options!;
        var priceAnswer = priceChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                priceOptions,
                priceAnswer,
                "fourteen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                priceOptions,
                priceAnswer,
                "forty").State);

        var openingOptions = openingChoice.Parameters["options"].Options!;
        var openingAnswer = openingChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                openingOptions,
                openingAnswer,
                "ten").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                openingOptions,
                openingAnswer,
                "nine").State);

        var appointmentOptions = appointmentChoice.Parameters["options"].Options!;
        var appointmentAnswer = appointmentChoice.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                appointmentOptions,
                appointmentAnswer,
                "tuesday-ten").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                appointmentOptions,
                appointmentAnswer,
                "wednesday-ten").State);

        var missionResponses = missionScenario.Parameters["responses"].Options!;
        var missionAnswer = missionScenario.Parameters["answer"].Value!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionResponses,
                missionAnswer,
                "shared").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionResponses,
                missionAnswer,
                "tuesday").State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "read").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read"],
                "propose").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read", "choose", "propose"],
                "confirm").State);
    }

    [TestMethod]
    public void UnitFourActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit04");
        var familyArticle = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("article-stamp"));
        var identityOrder = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var ageGap = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var appearanceChoice = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("picture-match"));
        var characterNote = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("note-write"));
        var possessiveGap = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var hearing = unit.Lessons[6];
        var detailOrder = hearing.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var soundChoice = hearing.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("minimal-pair-doors"));
        var profileForm = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var introductionScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var mission = unit.Lessons[9];
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        Assert.IsFalse(unit.Lessons.SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .Any(parameter => parameter.Kind == TemplateParameterKind.AssetReference));

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                familyArticle.Parameters["options"].Options!,
                familyArticle.Parameters["answer"].Value!,
                "die").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                familyArticle.Parameters["options"].Options!,
                familyArticle.Parameters["answer"].Value!,
                "der").State);

        var identityOptions = identityOrder.Parameters["options"].Options!;
        var identityIds = identityOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(identityOptions, identityIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                identityOptions,
                identityIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                ageGap.Parameters["options"].Options!,
                ageGap.Parameters["answer"].Value!,
                "twelve").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                ageGap.Parameters["options"].Options!,
                ageGap.Parameters["answer"].Value!,
                "eleven").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                appearanceChoice.Parameters["options"].Options!,
                appearanceChoice.Parameters["answer"].Value!,
                "brown").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePictureMatch(
                appearanceChoice.Parameters["options"].Options!,
                appearanceChoice.Parameters["answer"].Value!,
                "blond").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                characterNote.Parameters["required-content"].Options!,
                "Karim ist freundlich und ruhig.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                characterNote.Parameters["required-content"].Options!,
                "Karim ist freundlich.").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                possessiveGap.Parameters["options"].Options!,
                possessiveGap.Parameters["answer"].Value!,
                "mein").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                possessiveGap.Parameters["options"].Options!,
                possessiveGap.Parameters["answer"].Value!,
                "meine").State);

        var detailEvents = detailOrder.Parameters["events"].Options!;
        var detailIds = detailEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(detailEvents, detailIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                detailEvents,
                [detailIds[1], detailIds[0], detailIds[2]]).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                soundChoice.Parameters["options"].Options!,
                soundChoice.Parameters["answer"].Value!,
                "bruder").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                soundChoice.Parameters["options"].Options!,
                soundChoice.Parameters["answer"].Value!,
                "bude").State);

        var profileAnswers = profileForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                profileAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Lina",
                    ["occupation"] = "Ärztin",
                    ["age"] = "dreißig",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                profileAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Lina",
                    ["occupation"] = "Lehrerin",
                    ["age"] = "dreißig",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                introductionScenario.Parameters["responses"].Options!,
                introductionScenario.Parameters["answer"].Value!,
                "complete").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                introductionScenario.Parameters["responses"].Options!,
                introductionScenario.Parameters["answer"].Value!,
                "name-only").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "complete").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "wrong-relation").State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "read").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read"],
                "name").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read", "relationship", "name"],
                "detail").State);
    }

    [TestMethod]
    public void UnitFiveActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit05");
        var actionChoice = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-match"));
        var regularWheel = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("conjugation-wheel"));
        var irregularGap = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var split = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("separable-verb-split"));
        var morning = unit.Lessons[4];
        var breakfastTime = morning.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("schedule-read"));
        var morningOrder = morning.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var preferenceSort = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sort-into-baskets"));
        var scheduleTyping = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-type"));
        var calendarForm = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var comparisonScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var mission = unit.Lessons[9];
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        Assert.IsFalse(unit.Lessons.SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .Any(parameter => parameter.Kind == TemplateParameterKind.AssetReference));

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                actionChoice.Parameters["options"].Options!,
                actionChoice.Parameters["answer"].Value!,
                "get-up").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                actionChoice.Parameters["options"].Options!,
                actionChoice.Parameters["answer"].Value!,
                "sleep").State);

        var persons = regularWheel.Parameters["persons"].Options!;
        var forms = regularWheel.Parameters["forms"].Options!;
        var formAnswers = regularWheel.Parameters["answers"].Options!
            .ToDictionary(option => option.Id, option => option.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                persons,
                forms,
                formAnswers,
                "du",
                "machst").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                persons,
                forms,
                formAnswers,
                "du",
                "mache").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                irregularGap.Parameters["options"].Options!,
                irregularGap.Parameters["answer"].Value!,
                "liest").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                irregularGap.Parameters["options"].Options!,
                irregularGap.Parameters["answer"].Value!,
                "lese").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateAcknowledgement(acknowledged: true).State);
        Assert.AreEqual("auf.", split.Parameters["prefix"].Value);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                breakfastTime.Parameters["options"].Options!,
                breakfastTime.Parameters["answer"].Value!,
                "half-eight").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                breakfastTime.Parameters["options"].Options!,
                breakfastTime.Parameters["answer"].Value!,
                "eight").State);

        var morningEvents = morningOrder.Parameters["events"].Options!;
        var morningIds = morningEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(morningEvents, morningIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                morningEvents,
                morningIds.Reverse().ToArray()).State);

        var preferenceItems = preferenceSort.Parameters["items"].Options!;
        var preferenceBaskets = preferenceSort.Parameters["baskets"].Options!;
        var preferenceAnswers = preferenceSort.Parameters["answers"].Options!
            .ToDictionary(option => option.Id, option => option.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                preferenceItems,
                preferenceBaskets,
                preferenceAnswers,
                preferenceAnswers).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                preferenceItems,
                preferenceBaskets,
                preferenceAnswers,
                new Dictionary<string, string>(preferenceAnswers, StringComparer.Ordinal)
                {
                    ["tv"] = "like",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                scheduleTyping.Parameters["accepted-answers"].Options!,
                "acht Uhr").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                scheduleTyping.Parameters["accepted-answers"].Options!,
                " ").State);

        var calendarAnswers = calendarForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                calendarAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["day"] = "Dienstag",
                    ["time"] = "Abend",
                    ["activity"] = "frei",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                calendarAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["day"] = "Montag",
                    ["time"] = "Abend",
                    ["activity"] = "frei",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                comparisonScenario.Parameters["responses"].Options!,
                comparisonScenario.Parameters["answer"].Value!,
                "evening").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                comparisonScenario.Parameters["responses"].Options!,
                comparisonScenario.Parameters["answer"].Value!,
                "morning").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "coffee").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "work").State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "read").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read"],
                "propose").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read", "describe", "propose"],
                "confirm").State);
    }

    [TestMethod]
    public void UnitSixActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit06");
        var itemPairs = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("pair-cards"));
        var requestOrder = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var politeScenario = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var caseSwitch = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("case-switchboard"));
        var availabilityScenario = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var negation = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("negation-strike"));
        var listening = unit.Lessons[6];
        var listeningOrder = listening.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var listeningPrice = listening.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-price-tag"));
        var menu = unit.Lessons[7];
        var menuChoice = menu.TemplateInstances
            .First(instance => instance.TemplateId == new TemplateId("menu-read"));
        var menuForm = menu.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var groupScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var mission = unit.Lessons[9];
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionBill = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var allowedAssets = new HashSet<string>(StringComparer.Ordinal)
        {
            "asset.de.cafe.coffee",
            "asset.de.cafe.tea",
            "asset.de.cafe.water",
            "asset.de.stage.market-backdrop",
        };
        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .Select(assetId => assetId!)
            .ToArray();
        Assert.IsNotEmpty(referencedAssets);
        Assert.IsTrue(referencedAssets.All(allowedAssets.Contains));

        var pairs = itemPairs.Parameters["pairs"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:coffee", "image:coffee"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:coffee", "image:tea"]).State);

        var requestPieces = requestOrder.Parameters["options"].Options!;
        var requestIds = requestPieces.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(requestPieces, requestIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                requestPieces,
                requestIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                politeScenario.Parameters["responses"].Options!,
                politeScenario.Parameters["answer"].Value!,
                "complete").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                politeScenario.Parameters["responses"].Options!,
                politeScenario.Parameters["answer"].Value!,
                "bare").State);

        var caseAnswers = caseSwitch.Parameters["answers"].Options!
            .ToDictionary(option => option.Id, option => option.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                caseSwitch.Parameters["roles"].Options!,
                caseSwitch.Parameters["articles"].Options!,
                caseAnswers,
                "direct-object",
                "einen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                caseSwitch.Parameters["roles"].Options!,
                caseSwitch.Parameters["articles"].Options!,
                caseAnswers,
                "direct-object",
                "der").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                availabilityScenario.Parameters["responses"].Options!,
                availabilityScenario.Parameters["answer"].Value!,
                "cake").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                availabilityScenario.Parameters["responses"].Options!,
                availabilityScenario.Parameters["answer"].Value!,
                "wrong-order").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negation.Parameters["negators"].Options!,
                negation.Parameters["slots"].Options!,
                negation.Parameters["answer-negator"].Value!,
                negation.Parameters["answer-slot"].Value!,
                "keinen",
                "before-object").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSelectionPair(
                negation.Parameters["negators"].Options!,
                negation.Parameters["slots"].Options!,
                negation.Parameters["answer-negator"].Value!,
                negation.Parameters["answer-slot"].Value!,
                "nicht",
                "before-object").State);

        var orderEvents = listeningOrder.Parameters["events"].Options!;
        var orderEventIds = orderEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(orderEvents, orderEventIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                orderEvents,
                orderEventIds.Reverse().ToArray()).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                listeningPrice.Parameters["options"].Options!,
                listeningPrice.Parameters["answer"].Value!,
                "720").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                menuChoice.Parameters["options"].Options!,
                menuChoice.Parameters["answer"].Value!,
                "340").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                menuChoice.Parameters["options"].Options!,
                menuChoice.Parameters["answer"].Value!,
                "420").State);

        var menuAnswers = menuForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                menuAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["item"] = "Tee",
                    ["quantity"] = "eine Tasse",
                    ["price"] = "3,40 €",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                menuAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["item"] = "Kaffee",
                    ["quantity"] = "eine Tasse",
                    ["price"] = "3,40 €",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                groupScenario.Parameters["responses"].Options!,
                groupScenario.Parameters["answer"].Value!,
                "complete").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                groupScenario.Parameters["responses"].Options!,
                groupScenario.Parameters["answer"].Value!,
                "swapped").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "coffee").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "bare").State);

        var billAnswers = missionBill.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                billAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["item"] = "Kaffee",
                    ["quantity"] = "eins",
                    ["total"] = "2,80 €",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                billAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["item"] = "Kaffee",
                    ["quantity"] = "zwei",
                    ["total"] = "2,80 €",
                }).State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "menu").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["menu"],
                "clarify").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["menu", "order", "clarify"],
                "pay").State);
    }

    [TestMethod]
    public void UnitSevenActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit07");
        var roomPairs = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("pair-cards"));
        var furnitureSort = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sort-into-baskets"));
        var esGibt = unit.Lessons[2];
        var esGibtOrder = esGibt.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var esGibtCase = esGibt.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("case-switchboard"));
        var location = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("preposition-stage"));
        var keyScenario = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var listeningOrder = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var rental = unit.Lessons[7];
        var rentalSign = rental.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var rentalForm = rental.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var layoutScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var mission = unit.Lessons[9];
        var missionSign = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionForm = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .ToArray();
        Assert.IsEmpty(referencedAssets);
        StringAssert.Contains(unit.Manifest.Review.Notes, "named asset follow-ups");

        var pairs = roomPairs.Parameters["pairs"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:living", "image:living"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:living", "image:sleeping"]).State);

        var sortAnswers = furnitureSort.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                furnitureSort.Parameters["items"].Options!,
                furnitureSort.Parameters["baskets"].Options!,
                sortAnswers,
                new Dictionary<string, string>(sortAnswers, StringComparer.Ordinal)).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                furnitureSort.Parameters["items"].Options!,
                furnitureSort.Parameters["baskets"].Options!,
                sortAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["kitchen"] = "furniture",
                    ["bedroom"] = "rooms",
                    ["sofa"] = "furniture",
                    ["lamp"] = "furniture",
                }).State);

        var orderOptions = esGibtOrder.Parameters["options"].Options!;
        var orderIds = orderOptions.Select(candidate => candidate.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(orderOptions, orderIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                orderOptions,
                orderIds.Reverse().ToArray()).State);

        var caseAnswers = esGibtCase.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                esGibtCase.Parameters["roles"].Options!,
                esGibtCase.Parameters["articles"].Options!,
                caseAnswers,
                "direct-object",
                "einen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                esGibtCase.Parameters["roles"].Options!,
                esGibtCase.Parameters["articles"].Options!,
                caseAnswers,
                "direct-object",
                "der").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                location.Parameters["positions"].Options!,
                location.Parameters["answer"].Value!,
                "on").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                location.Parameters["positions"].Options!,
                location.Parameters["answer"].Value!,
                "beside").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                keyScenario.Parameters["responses"].Options!,
                keyScenario.Parameters["answer"].Value!,
                "table").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                keyScenario.Parameters["responses"].Options!,
                keyScenario.Parameters["answer"].Value!,
                "bag").State);

        var listeningEvents = listeningOrder.Parameters["events"].Options!;
        var listeningEventIds = listeningEvents.Select(candidate => candidate.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                listeningEvents,
                listeningEventIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                listeningEvents,
                listeningEventIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                rentalSign.Parameters["options"].Options!,
                rentalSign.Parameters["answer"].Value!,
                "rent-780").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                rentalSign.Parameters["options"].Options!,
                rentalSign.Parameters["answer"].Value!,
                "rent-680").State);

        var rentalAnswers = rentalForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                rentalAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["rooms"] = "zwei",
                    ["rent"] = "780 Euro",
                    ["feature"] = "Balkon",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                rentalAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["rooms"] = "drei",
                    ["rent"] = "780 Euro",
                    ["feature"] = "Balkon",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                layoutScenario.Parameters["responses"].Options!,
                layoutScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                layoutScenario.Parameters["responses"].Options!,
                layoutScenario.Parameters["answer"].Value!,
                "swapped").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                missionSign.Parameters["options"].Options!,
                missionSign.Parameters["answer"].Value!,
                "keys").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                missionSign.Parameters["options"].Options!,
                missionSign.Parameters["answer"].Value!,
                "lamp").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "help").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "fragment").State);

        var missionAnswers = missionForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["room"] = "Wohnzimmer",
                    ["object"] = "Schlüssel",
                    ["location"] = "auf dem Tisch",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["room"] = "Wohnzimmer",
                    ["object"] = "Lampe",
                    ["location"] = "auf dem Tisch",
                }).State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "read").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read"],
                "locate").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["read", "describe", "locate"],
                "request").State);
    }

    [TestMethod]
    public void UnitEightActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit08");
        var placePairs = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("pair-cards"));
        var askOrder = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var directionRoute = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-route"));
        var transportCase = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("case-switchboard"));
        var station = unit.Lessons[6];
        var stationSign = station.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var stationForm = station.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var displaySign = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var visitorScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var mission = unit.Lessons[9];
        var missionSign = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var missionRoute = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-route"));
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionForm = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var missionCapstone = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .ToArray();
        Assert.IsEmpty(referencedAssets);
        StringAssert.Contains(unit.Manifest.Review.Notes, "named asset follow-ups");

        var pairs = placePairs.Parameters["pairs"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:station", "image:station"]).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluatePairCards(
                pairs,
                ["word:station", "image:library"]).State);

        var questionOptions = askOrder.Parameters["options"].Options!;
        var questionIds = questionOptions.Select(candidate => candidate.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(questionOptions, questionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                questionOptions,
                questionIds.Reverse().ToArray()).State);

        var directionSteps = directionRoute.Parameters["route"].Options!;
        var directionIds = directionSteps.Select(candidate => candidate.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(directionSteps, directionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                directionSteps,
                directionIds.Reverse().ToArray()).State);

        var caseAnswers = transportCase.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                transportCase.Parameters["roles"].Options!,
                transportCase.Parameters["articles"].Options!,
                caseAnswers,
                "after-mit",
                "dem").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                transportCase.Parameters["roles"].Options!,
                transportCase.Parameters["articles"].Options!,
                caseAnswers,
                "after-mit",
                "der").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                stationSign.Parameters["options"].Options!,
                stationSign.Parameters["answer"].Value!,
                "platform-3").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                stationSign.Parameters["options"].Options!,
                stationSign.Parameters["answer"].Value!,
                "platform-8").State);

        var stationAnswers = stationForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                stationAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["destination"] = "Bonn",
                    ["platform"] = "3",
                    ["time"] = "10:15",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                stationAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["destination"] = "Bonn",
                    ["platform"] = "8",
                    ["time"] = "10:15",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                displaySign.Parameters["options"].Options!,
                displaySign.Parameters["answer"].Value!,
                "centre").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                displaySign.Parameters["options"].Options!,
                displaySign.Parameters["answer"].Value!,
                "north").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                visitorScenario.Parameters["responses"].Options!,
                visitorScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                visitorScenario.Parameters["responses"].Options!,
                visitorScenario.Parameters["answer"].Value!,
                "swapped").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                missionSign.Parameters["options"].Options!,
                missionSign.Parameters["answer"].Value!,
                "left").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                missionSign.Parameters["options"].Options!,
                missionSign.Parameters["answer"].Value!,
                "right").State);

        var missionRouteSteps = missionRoute.Parameters["route"].Options!;
        var missionRouteIds = missionRouteSteps.Select(candidate => candidate.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                missionRouteSteps,
                missionRouteIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                missionRouteSteps,
                missionRouteIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "fragment").State);

        var missionAnswers = missionForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["transport"] = "U-Bahn",
                    ["exit"] = "Rathaus",
                    ["turn"] = "links",
                    ["destination"] = "Museum",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["transport"] = "Bus",
                    ["exit"] = "Rathaus",
                    ["turn"] = "links",
                    ["destination"] = "Museum",
                }).State);

        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "ask").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["ask"],
                "travel").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["ask", "clarify", "travel"],
                "arrive").State);
    }

    [TestMethod]
    public void UnitNineActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit09");
        var personalForm = unit.Lessons[0].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var timeQuantity = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("number-tiles"));
        var timeSchedule = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("schedule-read"));
        var actionOrder = unit.Lessons[2].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var peopleArticle = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("article-stamp"));
        var peopleCase = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("case-switchboard"));
        var noticeSign = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var messageNote = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("note-write"));
        var exchangeChoice = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("dialogue-eavesdrop"));
        var exchangeOrder = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-order"));
        var repairGap = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var repairResponse = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("prompt-respond"));
        var mediationRoute = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-route"));
        var mediationForm = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var mediationScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var capstone = unit.Lessons[9];
        var capstoneSign = capstone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sign-reading"));
        var capstoneRoute = capstone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-route"));
        var capstoneScenario = capstone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var capstoneForm = capstone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var capstoneSteps = capstone.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));

        Assert.HasCount(80, unit.Lessons.SelectMany(lesson => lesson.TemplateInstances));
        Assert.HasCount(
            80,
            unit.Lessons
                .SelectMany(lesson => lesson.TemplateInstances)
                .Select(instance => instance.Id)
                .Distinct(StringComparer.Ordinal));
        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .ToArray();
        Assert.IsEmpty(referencedAssets);
        StringAssert.Contains(unit.Manifest.Review.Notes, "named asset follow-ups");

        var personalAnswers = personalForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                personalAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Mina",
                    ["origin"] = "Indien",
                    ["languages"] = "Hindi und Englisch",
                    ["phone"] = "0176 4298",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                personalAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Mina",
                    ["origin"] = "Indien",
                    ["languages"] = "Deutsch",
                    ["phone"] = "0176 4298",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                timeQuantity.Parameters["options"].Options!,
                timeQuantity.Parameters["answer"].Value!,
                "eight").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                timeQuantity.Parameters["options"].Options!,
                timeQuantity.Parameters["answer"].Value!,
                "eighteen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                timeSchedule.Parameters["options"].Options!,
                timeSchedule.Parameters["answer"].Value!,
                "ten").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                timeSchedule.Parameters["options"].Options!,
                timeSchedule.Parameters["answer"].Value!,
                "twelve").State);

        var actionOptions = actionOrder.Parameters["options"].Options!;
        var actionIds = actionOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(actionOptions, actionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                actionOptions,
                actionIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                peopleArticle.Parameters["options"].Options!,
                peopleArticle.Parameters["answer"].Value!,
                "my-f").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                peopleArticle.Parameters["options"].Options!,
                peopleArticle.Parameters["answer"].Value!,
                "my-m").State);
        var caseAnswers = peopleCase.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                peopleCase.Parameters["roles"].Options!,
                peopleCase.Parameters["articles"].Options!,
                caseAnswers,
                "object",
                "einen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateMappedPair(
                peopleCase.Parameters["roles"].Options!,
                peopleCase.Parameters["articles"].Options!,
                caseAnswers,
                "object",
                "der").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                noticeSign.Parameters["options"].Options!,
                noticeSign.Parameters["answer"].Value!,
                "station").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                noticeSign.Parameters["options"].Options!,
                noticeSign.Parameters["answer"].Value!,
                "cafe").State);

        var requiredMessage = messageNote.Parameters["required-content"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredMessage,
                "Hallo Lea, ich komme am Dienstag um zehn Uhr zum Bahnhof und bringe die Fahrkarte mit.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                requiredMessage,
                "Hallo Lea, bis bald!").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                exchangeChoice.Parameters["options"].Options!,
                exchangeChoice.Parameters["answer"].Value!,
                "ten").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                exchangeChoice.Parameters["options"].Options!,
                exchangeChoice.Parameters["answer"].Value!,
                "eight").State);
        var exchangeEvents = exchangeOrder.Parameters["events"].Options!;
        var exchangeIds = exchangeEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(exchangeEvents, exchangeIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                exchangeEvents,
                exchangeIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                repairGap.Parameters["options"].Options!,
                repairGap.Parameters["answer"].Value!,
                "again").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                repairGap.Parameters["options"].Options!,
                repairGap.Parameters["answer"].Value!,
                "slow").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                repairResponse.Parameters["accepted-responses"].Options!,
                "Bitte sprechen Sie langsamer.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                repairResponse.Parameters["accepted-responses"].Options!,
                " ").State);

        var mediationSteps = mediationRoute.Parameters["route"].Options!;
        var mediationIds = mediationSteps.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(mediationSteps, mediationIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                mediationSteps,
                mediationIds.Reverse().ToArray()).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                mediationScenario.Parameters["responses"].Options!,
                mediationScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                mediationScenario.Parameters["responses"].Options!,
                mediationScenario.Parameters["answer"].Value!,
                "swapped").State);
        var mediationAnswers = mediationForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                mediationAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Omar",
                    ["day"] = "Dienstag",
                    ["time"] = "10 Uhr",
                    ["start"] = "Bahnhof",
                    ["end"] = "Café",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                mediationAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Lea",
                    ["day"] = "Dienstag",
                    ["time"] = "10 Uhr",
                    ["start"] = "Bahnhof",
                    ["end"] = "Café",
                }).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                capstoneSign.Parameters["options"].Options!,
                capstoneSign.Parameters["answer"].Value!,
                "station").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                capstoneSign.Parameters["options"].Options!,
                capstoneSign.Parameters["answer"].Value!,
                "museum").State);
        var capstoneRouteSteps = capstoneRoute.Parameters["route"].Options!;
        var capstoneRouteIds = capstoneRouteSteps.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                capstoneRouteSteps,
                capstoneRouteIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                capstoneRouteSteps,
                capstoneRouteIds.Reverse().ToArray()).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                capstoneScenario.Parameters["responses"].Options!,
                capstoneScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                capstoneScenario.Parameters["responses"].Options!,
                capstoneScenario.Parameters["answer"].Value!,
                "fragment").State);
        var capstoneAnswers = capstoneForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                capstoneAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Mina",
                    ["time"] = "10 Uhr",
                    ["meeting"] = "Bahnhof",
                    ["destination"] = "Museum",
                    ["order"] = "einen Kaffee",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                capstoneAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["name"] = "Mina",
                    ["time"] = "10 Uhr",
                    ["meeting"] = "Café",
                    ["destination"] = "Museum",
                    ["order"] = "einen Kaffee",
                }).State);

        var steps = capstoneSteps.Parameters["steps"].Options!;
        var chain = capstoneSteps.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                [],
                "introduce").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["introduce"],
                "travel").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["introduce", "confirm", "travel"],
                "order").State);
    }

    [TestMethod]
    public void UnitTenActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a2.unit10");
        var regularSpelling = unit.Lessons[1].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("spelling-tiles"));
        var auxiliarySort = unit.Lessons[3].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("sort-into-baskets"));
        var yesterdayOrder = unit.Lessons[4].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("word-order-train"));
        var followUpGap = unit.Lessons[5].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("gap-card"));
        var weekendDictation = unit.Lessons[6].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-type"));
        var updateNote = unit.Lessons[7].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("note-write"));
        var retellingScenario = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var retellingForm = unit.Lessons[8].TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var mission = unit.Lessons[9];
        var missionRoute = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("listen-route"));
        var missionScenario = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("scenario-theatre"));
        var missionForm = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("form-fill"));
        var missionSteps = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("unit-capstone"));
        var missionReadAloud = mission.TemplateInstances
            .Single(instance => instance.TemplateId == new TemplateId("read-aloud-card"));

        Assert.HasCount(80, unit.Lessons.SelectMany(lesson => lesson.TemplateInstances));
        Assert.HasCount(
            80,
            unit.Lessons
                .SelectMany(lesson => lesson.TemplateInstances)
                .Select(instance => instance.Id)
                .Distinct(StringComparer.Ordinal));
        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .ToArray();
        Assert.IsEmpty(referencedAssets);
        StringAssert.Contains(unit.Manifest.Review.Notes, "named asset follow-ups");

        var letters = regularSpelling.Parameters["letters"].Options!;
        var letterIds = letters.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(letters, letterIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                letters,
                letterIds.Reverse().ToArray()).State);

        var sortItems = auxiliarySort.Parameters["items"].Options!;
        var sortBaskets = auxiliarySort.Parameters["baskets"].Options!;
        var expectedSort = auxiliarySort.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                sortItems,
                sortBaskets,
                expectedSort,
                expectedSort).State);
        var wrongSort = new Dictionary<string, string>(expectedSort, StringComparer.Ordinal)
        {
            ["travelled"] = "haben",
        };
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                sortItems,
                sortBaskets,
                expectedSort,
                wrongSort).State);

        var yesterdayOptions = yesterdayOrder.Parameters["options"].Options!;
        var yesterdayIds = yesterdayOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                yesterdayOptions,
                yesterdayIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                yesterdayOptions,
                yesterdayIds.Reverse().ToArray()).State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                followUpGap.Parameters["options"].Options!,
                followUpGap.Parameters["answer"].Value!,
                "when").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                followUpGap.Parameters["options"].Options!,
                followUpGap.Parameters["answer"].Value!,
                "where").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                weekendDictation.Parameters["accepted-answers"].Options!,
                "Am Sonntag habe ich lange geschlafen und Musik gehört.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                weekendDictation.Parameters["accepted-answers"].Options!,
                " ").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                updateNote.Parameters["required-content"].Options!,
                "Zuerst habe ich Mira getroffen. Danach haben wir ein Museum besucht.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateRequiredContent(
                updateNote.Parameters["required-content"].Options!,
                "Ich habe Mira im Museum getroffen.").State);

        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                retellingScenario.Parameters["responses"].Options!,
                retellingScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                retellingScenario.Parameters["responses"].Options!,
                retellingScenario.Parameters["answer"].Value!,
                "swapped").State);
        var retellingAnswers = retellingForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                retellingAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Lea",
                    ["day"] = "Samstag",
                    ["city"] = "Bonn",
                    ["relative"] = "Tante",
                    ["place"] = "Park",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                retellingAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Lea",
                    ["day"] = "Samstag",
                    ["city"] = "Berlin",
                    ["relative"] = "Tante",
                    ["place"] = "Park",
                }).State);

        var route = missionRoute.Parameters["route"].Options!;
        var routeIds = route.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(route, routeIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                route,
                routeIds.Reverse().ToArray()).State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "missing-question").State);
        var missionAnswers = missionForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Mina",
                    ["city"] = "Hamburg",
                    ["company"] = "Freunde",
                    ["return"] = "Sonntag",
                    ["contrast"] = "Omar zu Hause",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["person"] = "Mina",
                    ["city"] = "Köln",
                    ["company"] = "Freunde",
                    ["return"] = "Sonntag",
                    ["contrast"] = "Omar zu Hause",
                }).State);

        var steps = missionSteps.Parameters["steps"].Options!;
        var chain = missionSteps.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                [],
                "account").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["account"],
                "retell").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                steps,
                chain,
                ["account", "question", "retell"],
                "compare").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                missionReadAloud.Parameters["accepted-transcripts"].Options!,
                "Am Wochenende bin ich nach Hamburg gefahren. Ich habe Freunde getroffen und wir haben gekocht.").State);

        foreach (var recap in unit.Lessons
                     .SelectMany(lesson => lesson.TemplateInstances)
                     .Where(instance => instance.TemplateId == new TemplateId("recap-scrapbook")))
        {
            var actions = recap.Parameters["actions"].Options!;
            var acknowledgementId = recap.Parameters["acknowledgement"].Value!;
            Assert.IsTrue(actions.Any(action => action.Id == acknowledgementId));
            Assert.AreEqual(
                TemplateOutcomeState.Success,
                TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                    actions,
                    acknowledgementId,
                    acknowledgementId).State);
        }
    }

    [TestMethod]
    public void UnitElevenActivityAnswersMapDeterministically()
    {
        var unit = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.a2.unit11");
        TemplateInstance Instance(int lessonIndex, string templateId) =>
            unit.Lessons[lessonIndex].TemplateInstances
                .Single(instance => instance.TemplateId == new TemplateId(templateId));

        Assert.HasCount(80, unit.Lessons.SelectMany(lesson => lesson.TemplateInstances));
        Assert.HasCount(
            80,
            unit.Lessons
                .SelectMany(lesson => lesson.TemplateInstances)
                .Select(instance => instance.Id)
                .Distinct(StringComparer.Ordinal));
        var referencedAssets = unit.Lessons
            .SelectMany(lesson => lesson.TemplateInstances)
            .SelectMany(instance => instance.Parameters.Values)
            .SelectMany(parameter =>
                (parameter.Kind == TemplateParameterKind.AssetReference
                    ? new[] { parameter.Value }
                    : Array.Empty<string?>())
                .Concat(parameter.Options?.Select(option => option.AssetReferenceId) ?? []))
            .Where(assetId => assetId is not null)
            .ToArray();
        Assert.IsEmpty(referencedAssets);
        StringAssert.Contains(unit.Manifest.Review.Notes, "named asset follow-ups");

        var bodySort = Instance(0, "sort-into-baskets");
        var bodyItems = bodySort.Parameters["items"].Options!;
        var bodyBaskets = bodySort.Parameters["baskets"].Options!;
        var bodyAnswers = bodySort.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                bodyItems,
                bodyBaskets,
                bodyAnswers,
                bodyAnswers).State);
        var wrongBodyAnswers = new Dictionary<string, string>(bodyAnswers, StringComparer.Ordinal)
        {
            ["head"] = "das",
        };
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                bodyItems,
                bodyBaskets,
                bodyAnswers,
                wrongBodyAnswers).State);

        var symptomGap = Instance(1, "gap-card");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                symptomGap.Parameters["options"].Options!,
                symptomGap.Parameters["answer"].Value!,
                "dative").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                symptomGap.Parameters["options"].Options!,
                symptomGap.Parameters["answer"].Value!,
                "nominative").State);

        var durationOrder = Instance(2, "word-order-train");
        var durationOptions = durationOrder.Parameters["options"].Options!;
        var durationIds = durationOptions.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(durationOptions, durationIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                durationOptions,
                durationIds.Reverse().ToArray()).State);

        var appointmentDialogue = Instance(3, "dialogue-eavesdrop");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                appointmentDialogue.Parameters["options"].Options!,
                appointmentDialogue.Parameters["answer"].Value!,
                "tuesday-ten").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                appointmentDialogue.Parameters["options"].Options!,
                appointmentDialogue.Parameters["answer"].Value!,
                "monday-nine").State);

        var adviceSort = Instance(4, "sort-into-baskets");
        var adviceItems = adviceSort.Parameters["items"].Options!;
        var adviceBaskets = adviceSort.Parameters["baskets"].Options!;
        var adviceAnswers = adviceSort.Parameters["answers"].Options!
            .ToDictionary(answer => answer.Id, answer => answer.Label, StringComparer.Ordinal);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                adviceItems,
                adviceBaskets,
                adviceAnswers,
                adviceAnswers).State);
        var wrongAdviceAnswers = new Dictionary<string, string>(adviceAnswers, StringComparer.Ordinal)
        {
            ["confirm"] = "should",
        };
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSortAssignments(
                adviceItems,
                adviceBaskets,
                adviceAnswers,
                wrongAdviceAnswers).State);

        var medicineSign = Instance(5, "sign-reading");
        StringAssert.Contains(medicineSign.Parameters["sign-text"].Value!, "NICHT EINNEHMEN");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                medicineSign.Parameters["options"].Options!,
                medicineSign.Parameters["answer"].Value!,
                "after-food").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                medicineSign.Parameters["options"].Options!,
                medicineSign.Parameters["answer"].Value!,
                "one").State);

        var receptionOrder = Instance(6, "listen-order");
        var receptionEvents = receptionOrder.Parameters["events"].Options!;
        var receptionIds = receptionEvents.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(receptionEvents, receptionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                receptionEvents,
                receptionIds.Reverse().ToArray()).State);

        var labelSign = Instance(7, "sign-reading");
        StringAssert.Contains(labelSign.Parameters["sign-text"].Value!, "KEIN ECHTES MEDIKAMENT");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                labelSign.Parameters["options"].Options!,
                labelSign.Parameters["answer"].Value!,
                "practice").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateSingleSelection(
                labelSign.Parameters["options"].Options!,
                labelSign.Parameters["answer"].Value!,
                "medicine").State);
        var labelForm = Instance(7, "form-fill");
        var labelAnswers = labelForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                labelAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["warning"] = "nicht einnehmen",
                    ["frequency"] = "zweimal täglich",
                    ["time"] = "nach dem Essen",
                    ["contact"] = "Apotheke",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                labelAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["warning"] = "einnehmen",
                    ["frequency"] = "zweimal täglich",
                    ["time"] = "nach dem Essen",
                    ["contact"] = "Apotheke",
                }).State);

        var relayRoute = Instance(8, "listen-route");
        var relayStops = relayRoute.Parameters["route"].Options!;
        var relayIds = relayStops.Select(option => option.Id).ToArray();
        CollectionAssert.AreEqual(
            new[] { "person", "duration", "symptom", "appointment", "items" },
            relayIds);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(relayStops, relayIds).State);
        var relayScenario = Instance(8, "scenario-theatre");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                relayScenario.Parameters["responses"].Options!,
                relayScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                relayScenario.Parameters["responses"].Options!,
                relayScenario.Parameters["answer"].Value!,
                "diagnosis").State);

        var missionRoute = Instance(9, "listen-route");
        var missionStops = missionRoute.Parameters["route"].Options!;
        var missionIds = missionStops.Select(option => option.Id).ToArray();
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateWordOrder(missionStops, missionIds).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateWordOrder(
                missionStops,
                missionIds.Reverse().ToArray()).State);

        var missionScenario = Instance(9, "scenario-theatre");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "correct").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateScenarioChoice(
                missionScenario.Parameters["responses"].Options!,
                missionScenario.Parameters["answer"].Value!,
                "diagnosis").State);

        var missionForm = Instance(9, "form-fill");
        var missionAnswers = missionForm.Parameters["answers"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["symptom"] = "Hals",
                    ["duration"] = "seit gestern",
                    ["time"] = "elf Uhr",
                    ["arrival"] = "zehn Minuten früher",
                    ["item"] = "Karte",
                }).State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateTextFields(
                missionAnswers,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["symptom"] = "Hals",
                    ["duration"] = "seit gestern",
                    ["time"] = "zehn Uhr",
                    ["arrival"] = "zehn Minuten früher",
                    ["item"] = "Karte",
                }).State);

        var missionCapstone = Instance(9, "unit-capstone");
        var missionSteps = missionCapstone.Parameters["steps"].Options!;
        var missionChain = missionCapstone.Parameters["template-chain"].Options!;
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                [],
                "symptom").State);
        Assert.AreEqual(
            TemplateOutcomeState.Failure,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["symptom"],
                "listen").State);
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateCapstoneStep(
                missionSteps,
                missionChain,
                ["symptom", "request", "listen"],
                "confirm").State);

        var missionReadAloud = Instance(9, "read-aloud-card");
        Assert.AreEqual(
            TemplateOutcomeState.Success,
            TemplateInteractionEvaluator.EvaluateDictation(
                missionReadAloud.Parameters["accepted-transcripts"].Options!,
                "Der Termin ist um elf Uhr. Ich komme zehn Minuten früher und bringe meine Karte mit.").State);
        Assert.AreEqual(
            TemplateOutcomeState.Uncertain,
            TemplateInteractionEvaluator.EvaluateDictation(
                missionReadAloud.Parameters["accepted-transcripts"].Options!,
                " ").State);

        foreach (var recap in unit.Lessons
                     .SelectMany(lesson => lesson.TemplateInstances)
                     .Where(instance => instance.TemplateId == new TemplateId("recap-scrapbook")))
        {
            var actions = recap.Parameters["actions"].Options!;
            var acknowledgementId = recap.Parameters["acknowledgement"].Value!;
            Assert.IsTrue(actions.Any(action => action.Id == acknowledgementId));
            Assert.AreEqual(
                TemplateOutcomeState.Success,
                TemplateInteractionEvaluator.EvaluateAdvisoryChoice(
                    actions,
                    acknowledgementId,
                    acknowledgementId).State);
        }
    }

    [TestMethod]
    public void SchemaFourUsesExplicitCourseOrderAndTenLessonUnits()
    {
        var target = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var greeting = target.Concepts[0] with
        {
            Examples = Replace(
                target.Concepts[0].Examples,
                0,
                target.Concepts[0].Examples[0] with { Id = "de.example.course.greeting" }),
        };
        var pronoun = target.Concepts[1] with
        {
            Examples = Replace(
                target.Concepts[1].Examples,
                0,
                target.Concepts[1].Examples[0] with { Id = "de.example.course.pronoun" }),
        };
        target = target with
        {
            Manifest = target.Manifest with { SchemaVersion = 4 },
            Concepts = Replace(Replace(target.Concepts, 0, greeting), 1, pronoun),
            CourseUnits = [CourseUnitFixture(target)],
            Lessons =
            [
                new LessonTemplateContent(
                    $"lesson.{pronoun.Id}",
                    [ObjectSpotlightInstance(
                        $"lesson.{pronoun.Id}",
                        1,
                        pronoun,
                        "Ich",
                        "de.example.course.pronoun")],
                    CourseOrder: 2),
                new LessonTemplateContent(
                    $"lesson.{greeting.Id}",
                    [ObjectSpotlightInstance(
                        $"lesson.{greeting.Id}",
                        1,
                        greeting,
                        "Hallo",
                        "de.example.course.greeting")],
                    CourseOrder: 1),
            ],
        };
        var directory = WritePacks([target]);
        try
        {
            var course = ContentPackLoader
                .LoadDirectory(directory, ContentLoadPolicy.AuthoringPreview)
                .CreateCourseCatalog(new LanguageCode("de"), new LanguageCode("en"));

            Assert.HasCount(2, course.Units[0].Lessons);
            Assert.AreEqual("Meet and greet", course.Units[0].Title);
            Assert.AreEqual("lesson.de.function.greeting-basic", course.Units[0].Lessons[0].Id);
            Assert.AreEqual("lesson.de.pronoun.ich", course.Units[0].Lessons[1].Id);
            Assert.AreEqual(new VersionId("course-catalog-v2"), course.Version);
            Assert.AreEqual(10, CourseCatalogConfiguration.Default.LessonsPerUnit);
            Assert.AreEqual(448, course.RemainingLessonCount);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void SchemaFourLessonCanBindAConceptFromADeclaredDependency()
    {
        var bundled = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var greeting = bundled.Concepts[0] with
        {
            Examples = Replace(
                bundled.Concepts[0].Examples,
                0,
                bundled.Concepts[0].Examples[0] with { Id = "de.example.course.greeting" }),
        };
        var core = bundled with
        {
            Manifest = bundled.Manifest with { SchemaVersion = 4 },
            Concepts = Replace(bundled.Concepts, 0, greeting),
            Lessons = [],
            CourseUnits = [],
        };
        var unitSource = bundled.Sources[0] with { Id = "source.de.a1.unit01.fixture" };
        var unitConcept = bundled.Concepts[0] with
        {
            Id = "de.a1.unit01.fixture",
            Examples = bundled.Concepts[0].Examples
                .Select(example => example with { Id = null })
                .ToArray(),
            SourceIds = [unitSource.Id],
        };
        var unitPack = bundled with
        {
            Manifest = bundled.Manifest with
            {
                Id = "language.de.a1.unit01",
                SchemaVersion = 4,
                Dependencies =
                [
                    new PackDependency(
                        core.Manifest.Id,
                        core.Manifest.Version,
                        core.Manifest.Version),
                ],
            },
            Sources = [unitSource],
            Concepts = [unitConcept],
            Lexicon = [],
            Tasks = [],
            ErrorRules = [],
            FeedbackTemplates = [],
            Rubrics = [],
            PronunciationUtterances = [],
            Lessons =
            [
                new LessonTemplateContent(
                    $"lesson.{greeting.Id}",
                    [ObjectSpotlightInstance(
                        $"lesson.{greeting.Id}",
                        1,
                        greeting,
                        "Hallo",
                        "de.example.course.greeting")],
                    CourseOrder: 1),
            ],
            TransferMappings = [],
            CourseUnits =
            [
                CourseUnitFixture(bundled) with { SourceIds = [unitSource.Id] },
            ],
        };

        var errors = ContentPackValidator.Validate(
            [core, unitPack],
            ContentLoadPolicy.AuthoringPreview,
            LessonTemplateSchemas.All,
            []);
        var missingDependency = ContentPackValidator.Validate(
            [core, unitPack with
            {
                Manifest = unitPack.Manifest with { Dependencies = [] },
            }],
            ContentLoadPolicy.AuthoringPreview,
            LessonTemplateSchemas.All,
            []);

        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors));
        Assert.IsTrue(missingDependency.Any(error => error.Code == "dependency.missing"));
    }

    [TestMethod]
    public void SchemaFourRejectsMissingAndDuplicateCourseOrder()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var core = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.core");
        var target = catalog.Packs.Single(pack => pack.Manifest.Id == "language.de.a1.unit01");
        target = target with
        {
            Lessons =
            [
                target.Lessons[0] with { CourseOrder = 1 },
                target.Lessons[1] with { CourseOrder = 1 },
            ],
        };
        var assetIds = catalog.Assets.Select(asset => asset.Record.Id).ToArray();

        var duplicate = ContentPackValidator.Validate(
            [core, target],
            ContentLoadPolicy.AuthoringPreview,
            LessonTemplateSchemas.All,
            assetIds);
        var missing = ContentPackValidator.Validate(
            [core, target with
            {
                Lessons = [target.Lessons[0] with { CourseOrder = null }],
            }],
            ContentLoadPolicy.AuthoringPreview,
            LessonTemplateSchemas.All,
            assetIds);
        var empty = ContentPackValidator.Validate(
            [core, target with { Lessons = [] }],
            ContentLoadPolicy.AuthoringPreview);

        Assert.HasCount(2, duplicate.Where(error => error.Code == "course.order.duplicate"));
        Assert.IsTrue(missing.Any(error => error.Code == "course.order.missing"));
        Assert.IsTrue(empty.Any(error => error.Code == "course.lesson.missing"));
    }

    [TestMethod]
    public void BundledGermanCourseKeepsTargetContentStableAcrossAllInstructionLanguages()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var german = new LanguageCode("de");
        var english = catalog.CreateCourseCatalog(german, new LanguageCode("en"));
        var hindi = catalog.CreateCourseCatalog(german, new LanguageCode("hi"));
        var hinglish = catalog.CreateCourseCatalog(german, new LanguageCode("hi-latn"));

        CollectionAssert.AreEqual(
            new[]
            {
                new LanguageCode("en"),
                new LanguageCode("hi"),
                new LanguageCode("hi-latn"),
            },
            catalog.GetInstructionLanguages(german).ToArray());
        Assert.AreEqual(CoursePublicationState.Preview, english.PublicationState);
        Assert.AreEqual(CoursePublicationState.Preview, hindi.PublicationState);
        Assert.AreEqual(CoursePublicationState.Preview, hinglish.PublicationState);
        CollectionAssert.AreEqual(PresentationIds(english), PresentationIds(hindi));
        CollectionAssert.AreEqual(PresentationIds(english), PresentationIds(hinglish));
        Assert.AreEqual("Greet for the time of day", english.Units[0].Lessons[0].Title);
        Assert.AreEqual("दिन के समय के अनुसार अभिवादन करें", hindi.Units[0].Lessons[0].Title);
        Assert.AreEqual("Din ke samay ke hisaab se greet karein", hinglish.Units[0].Lessons[0].Title);
        Assert.AreEqual("Numbers, dates, and time", english.Units[2].Title);
        Assert.AreEqual("संख्याएँ, तारीखें और समय", hindi.Units[2].Title);
        Assert.AreEqual("Numbers, dates aur time", hinglish.Units[2].Title);
        Assert.AreEqual("Count and group objects", english.Units[2].Lessons[0].Title);
        Assert.AreEqual("वस्तुएँ गिनें और समूह बनाएँ", hindi.Units[2].Lessons[0].Title);
        Assert.AreEqual("Objects ginein aur groups banayein", hinglish.Units[2].Lessons[0].Title);
        Assert.AreEqual("People and family", english.Units[3].Title);
        Assert.AreEqual("लोग और परिवार", hindi.Units[3].Title);
        Assert.AreEqual("Log aur parivaar", hinglish.Units[3].Title);
        Assert.AreEqual("Name family members", english.Units[3].Lessons[0].Title);
        Assert.AreEqual("परिवार के सदस्यों के नाम बताएँ", hindi.Units[3].Lessons[0].Title);
        Assert.AreEqual("Family members ke naam batayein", hinglish.Units[3].Lessons[0].Title);
        Assert.AreEqual("Daily routines", english.Units[4].Title);
        Assert.AreEqual("रोज़ की दिनचर्या", hindi.Units[4].Title);
        Assert.AreEqual("Roz ki dincharya", hinglish.Units[4].Title);
        Assert.AreEqual("Name daily actions", english.Units[4].Lessons[0].Title);
        Assert.AreEqual("रोज़ के कामों के नाम बताएँ", hindi.Units[4].Lessons[0].Title);
        Assert.AreEqual("Daily actions ke naam batayein", hinglish.Units[4].Lessons[0].Title);
        Assert.AreEqual("Food and café visits", english.Units[5].Title);
        Assert.AreEqual("खाना और कैफ़े जाना", hindi.Units[5].Title);
        Assert.AreEqual("Food aur cafe visits", hinglish.Units[5].Title);
        Assert.AreEqual("Recognize café items", english.Units[5].Lessons[0].Title);
        Assert.AreEqual("कैफ़े की चीज़ें पहचानें", hindi.Units[5].Lessons[0].Title);
        Assert.AreEqual("Cafe items pehchanein", hinglish.Units[5].Lessons[0].Title);
        Assert.AreEqual("Home and belongings", english.Units[6].Title);
        Assert.AreEqual("घर और सामान", hindi.Units[6].Title);
        Assert.AreEqual("Ghar aur samaan", hinglish.Units[6].Title);
        Assert.AreEqual("Name rooms", english.Units[6].Lessons[0].Title);
        Assert.AreEqual("कमरों के नाम बताएँ", hindi.Units[6].Lessons[0].Title);
        Assert.AreEqual("Rooms ke naam batayein", hinglish.Units[6].Lessons[0].Title);
        Assert.AreEqual("Health and appointments", english.Units[10].Title);
        Assert.AreEqual("स्वास्थ्य और अपॉइंटमेंट", hindi.Units[10].Title);
        Assert.AreEqual("Health aur appointments", hinglish.Units[10].Title);
        Assert.AreEqual("Name body areas", english.Units[10].Lessons[0].Title);
        Assert.AreEqual("शरीर के हिस्सों के नाम बताएँ", hindi.Units[10].Lessons[0].Title);
        Assert.AreEqual("Body areas ke naam batayein", hinglish.Units[10].Lessons[0].Title);

        var englishLesson = english.Units.SelectMany(unit => unit.Lessons)
            .Single(lesson => lesson.Id == "lesson.de.a1.u01.greetings-by-time");
        var hindiLesson = hindi.Units.SelectMany(unit => unit.Lessons)
            .Single(lesson => lesson.Id == englishLesson.Id);
        var hinglishLesson = hinglish.Units.SelectMany(unit => unit.Lessons)
            .Single(lesson => lesson.Id == englishLesson.Id);
        var englishTemplates = englishLesson.Slides.Select(slide => slide.TemplateInstance!).ToArray();
        var hindiTemplates = hindiLesson.Slides.Select(slide => slide.TemplateInstance!).ToArray();
        var hinglishTemplates = hinglishLesson.Slides.Select(slide => slide.TemplateInstance!).ToArray();

        CollectionAssert.AreEqual(
            englishTemplates.Select(template => template.TemplateId.Value).ToArray(),
            hindiTemplates.Select(template => template.TemplateId.Value).ToArray());
        CollectionAssert.AreEqual(
            englishTemplates.Select(template => template.TemplateId.Value).ToArray(),
            hinglishTemplates.Select(template => template.TemplateId.Value).ToArray());
        Assert.AreEqual(
            englishTemplates[0].Parameters.Values["location"].Text,
            hindiTemplates[0].Parameters.Values["location"].Text);
        Assert.AreNotEqual(
            englishTemplates[0].Parameters.Values["instruction"].TextByLanguage!["en"],
            hindiTemplates[0].Parameters.Values["instruction"].TextByLanguage!["hi"]);
        Assert.AreEqual(
            "Do logon se milein aur dekhein ki greeting time ke saath kaise badalti hai.",
            hinglishTemplates[0].Parameters.Values["instruction"].TextByLanguage!["hi-latn"]);
        CollectionAssert.AreEqual(
            englishTemplates[0].Parameters.Values["cast"].Options!
                .Select(option => option.Label).ToArray(),
            hindiTemplates[0].Parameters.Values["cast"].Options!
                .Select(option => option.Label).ToArray());
        CollectionAssert.AreEqual(
            englishTemplates[3].Parameters.Values["options"].Options!
                .Select(option => option.Label).ToArray(),
            hindiTemplates[3].Parameters.Values["options"].Options!
                .Select(option => option.Label).ToArray());
    }

    [TestMethod]
    public void PreferenceChangeReroutesTheAlreadyLoadedCatalog()
    {
        var catalog = LoadBundled(ContentLoadPolicy.AuthoringPreview);
        var profile = new LearnerProfile(
            Guid.Parse("4beb45c3-57ed-47ee-ac8e-41f202e729d7"),
            new LanguageCode("de"),
            [
                new KnownLanguage(
                    new LanguageCode("en"),
                    LanguageProficiency.Advanced,
                    ComfortableReading: true,
                    ComfortableListening: true,
                    AllowExplanations: true),
                new KnownLanguage(
                    new LanguageCode("hi"),
                    LanguageProficiency.Advanced,
                    ComfortableReading: true,
                    ComfortableListening: true,
                    AllowExplanations: true),
                new KnownLanguage(
                    new LanguageCode("hi-latn"),
                    LanguageProficiency.Advanced,
                    ComfortableReading: true,
                    ComfortableListening: true,
                    AllowExplanations: true),
            ],
            new LearnerSettings(
                MultilingualShortcutMode.Automatic,
                PreferredExplanationLanguage: null,
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));

        var automatic = catalog.SelectInstructionLanguage(profile);
        var preferred = catalog.SelectInstructionLanguage(profile with
        {
            Settings = profile.Settings with
            {
                ShortcutMode = MultilingualShortcutMode.PreferredLanguage,
                PreferredExplanationLanguage = new LanguageCode("hi-latn"),
            },
        });

        Assert.AreEqual(new LanguageCode("en"), automatic.SelectedLanguage);
        Assert.AreEqual(new LanguageCode("hi-latn"), preferred.SelectedLanguage);
        var english = catalog.CreateCourseCatalog(profile.TargetLanguage, automatic.SelectedLanguage!.Value);
        var hinglish = catalog.CreateCourseCatalog(profile.TargetLanguage, preferred.SelectedLanguage!.Value);
        CollectionAssert.AreEqual(PresentationIds(english), PresentationIds(hinglish));
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
        var target = LoadBundled(ContentLoadPolicy.AuthoringPreview)
            .Packs.Single(pack => pack.Manifest.Id == "language.de.core");
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
            Manifest = target.Manifest with { SchemaVersion = 3 },
            CourseUnits = null,
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
        var directory = WritePacks([target]);
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
            Assert.HasCount(50, course.Units);
            Assert.IsTrue(course.Units.All(unit => unit.Lessons.Count == 10));
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
    [DataRow("broken-lesson-binding", "reference.broken")]
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
        string word,
        string exampleId = "de.example.catalog-fixture") =>
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
                        ["hi-latn"] = "greeting",
                    }),
                ["instruction"] = new(
                    TemplateParameterKind.TextByLanguage,
                    TextByLanguage: new Dictionary<string, string>
                    {
                        ["en"] = "Notice this greeting.",
                        ["hi"] = "नमस्ते देखें।",
                        ["hi-latn"] = "Is greeting ko dekhein.",
                    }),
                ["concept"] = new(TemplateParameterKind.ConceptReference, Value: concept.Id),
                ["example"] = new(
                    TemplateParameterKind.ExampleReference,
                    Value: exampleId),
            });

    private static CourseUnitContent CourseUnitFixture(ContentPackDocument target) =>
        new(
            "unit.de.a1.01",
            1,
            "A1",
            new Dictionary<string, string>
            {
                ["en"] = "Meet and greet",
                ["hi"] = "मिलें और अभिवादन करें",
                ["hi-latn"] = "Milein aur greet karein",
            },
            new Dictionary<string, string>
            {
                ["en"] = "Exchange greetings and basic personal information.",
                ["hi"] = "अभिवादन और बुनियादी व्यक्तिगत जानकारी साझा करें।",
                ["hi-latn"] = "Greetings aur basic personal information share karein.",
            },
            [target.Sources[0].Id],
            target.Manifest.Review);

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
            CourseUnits = pack.CourseUnits?
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
                    ["hi"] = $"कृत्रिम पाठ {index}",
                    ["hi-latn"] = $"Synthetic lesson {index}",
                },
                PrerequisiteIds = [],
                SuccessCriteria = seed.SuccessCriteria with { RequiredEvaluatorIds = [] },
                ErrorRuleIds = [],
            })
            .ToArray();

        return source with
        {
            Manifest = source.Manifest with { SchemaVersion = 3 },
            Concepts = concepts,
            Lexicon = [],
            Tasks = [],
            ErrorRules = [],
            FeedbackTemplates = [],
            Rubrics = [],
            PronunciationUtterances = [],
            Lessons = [],
            CourseUnits = null,
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
            Manifest = source.Manifest with
            {
                SchemaVersion = 3,
                InstructionLanguages = ["en", "hi"],
            },
            Concepts = [concept],
            Lexicon = [],
            Tasks = [],
            ErrorRules = [],
            FeedbackTemplates = [],
            Rubrics = [],
            PronunciationUtterances = [],
            Lessons = [],
            CourseUnits = null,
        };
    }

    private static IReadOnlyDictionary<string, string> AddHindi(
        IReadOnlyDictionary<string, string> values,
        string hindi)
    {
        var result = values.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        result["hi"] = hindi;
        return result;
    }

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
