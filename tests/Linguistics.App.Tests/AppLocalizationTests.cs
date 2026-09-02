using System.Collections;
using System.Globalization;
using System.Resources;
using Linguistics.App.Localization;
using Linguistics.Core.Curriculum;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Tests;

[TestClass]
[DoNotParallelize]
public sealed class AppLocalizationTests
{
    [TestMethod]
    public void EnglishAndHindiResourcesHaveCompleteMatchingKeys()
    {
        var resources = new ResourceManager(
            "Linguistics.App.Resources.AppStrings",
            typeof(AppStrings).Assembly);
        var english = Read(resources, CultureInfo.InvariantCulture);
        var hindi = Read(resources, CultureInfo.GetCultureInfo("hi"));

        CollectionAssert.AreEqual(english.Keys.ToArray(), hindi.Keys.ToArray());
        Assert.IsTrue(english.Values.All(value => !string.IsNullOrWhiteSpace(value)));
        Assert.IsTrue(hindi.Values.All(value => !string.IsNullOrWhiteSpace(value)));
        Assert.IsTrue(english.Values.Concat(hindi.Values).All(value =>
            !value.Contains('—')));
    }

    [TestMethod]
    public void AppStringsResolveTheSelectedCulture()
    {
        try
        {
            AppStrings.UseLanguage(new LanguageCode("hi"));

            Assert.AreEqual("ऐप की भाषा", AppStrings.Get("Settings_AppLanguage_Title"));
            Assert.AreEqual(new LanguageCode("hi"), AppStrings.CurrentLanguage);
            Assert.ThrowsExactly<ArgumentException>(() =>
                AppStrings.UseLanguage(new LanguageCode("fr")));
        }
        finally
        {
            AppStrings.UseLanguage(new LanguageCode("en"));
        }
    }

    [TestMethod]
    public void AppLanguageFollowsInstructionLanguageUnlessOverridden()
    {
        var profile = Profile();

        Assert.AreEqual(
            new LanguageCode("hi"),
            AppLanguageSelector.Select(profile, new LanguageCode("hi")));
        Assert.AreEqual(
            new LanguageCode("en"),
            AppLanguageSelector.Select(
                profile with
                {
                    Settings = profile.Settings with
                    {
                        AppLanguageOverride = new LanguageCode("en"),
                    },
                },
                new LanguageCode("hi")));
        Assert.AreEqual(
            new LanguageCode("en"),
            AppLanguageSelector.Select(profile, new LanguageCode("fr")));
    }

    [TestMethod]
    public void LearnChromeCoversEveryCourseConceptTypeInBothLanguages()
    {
        try
        {
            foreach (var language in AppStrings.SupportedLanguages)
            {
                AppStrings.UseLanguage(language);
                foreach (var type in Enum.GetValues<ConceptType>())
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(
                        AppStrings.Get($"Learn_Unit_{type}_Title")));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(
                        AppStrings.Get($"Learn_Unit_{type}_Description")));
                }
            }
        }
        finally
        {
            AppStrings.UseLanguage(new LanguageCode("en"));
        }
    }

    [TestMethod]
    public void LanguageChangeNotifiesLiveResourceBindings()
    {
        var notifications = 0;
        void Count(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Item[]")
            {
                notifications++;
            }
        }

        AppStringProvider.Instance.PropertyChanged += Count;
        try
        {
            AppStrings.UseLanguage(new LanguageCode("hi"));
            Assert.AreEqual(1, notifications);
        }
        finally
        {
            AppStringProvider.Instance.PropertyChanged -= Count;
            AppStrings.UseLanguage(new LanguageCode("en"));
        }
    }

    private static SortedDictionary<string, string> Read(
        ResourceManager resources,
        CultureInfo culture)
    {
        var set = resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new AssertFailedException($"Resource set '{culture.Name}' is missing.");
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in set.Cast<DictionaryEntry>())
        {
            values.Add((string)entry.Key, (string)entry.Value!);
        }

        return values;
    }

    private static LearnerProfile Profile() =>
        new(
            Guid.Parse("18cd0d39-389c-4eaf-977c-c3f22921daab"),
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
            ],
            new LearnerSettings(
                MultilingualShortcutMode.PreferredLanguage,
                new LanguageCode("hi"),
                MicrophonePreference.Later,
                RetainSpeechRecordings: false));
}
