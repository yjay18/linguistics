using Avalonia.Controls;
using Linguistics.Core.Content;
using Linguistics.Core.Profiles;

namespace Linguistics.App.Features.Learn.Templates;

internal delegate Control TemplateRendererFactory(
    ResolvedTemplateParameters parameters,
    LanguageCode instructionLanguage,
    bool shouldReduceMotion,
    Action<TemplateOutcome> reportOutcome);

internal sealed class TemplateRegistry
{
    private readonly IReadOnlyDictionary<TemplateId, TemplateRendererFactory> _renderers;

    public TemplateRegistry(IEnumerable<KeyValuePair<TemplateId, TemplateRendererFactory>> renderers)
    {
        ArgumentNullException.ThrowIfNull(renderers);

        var registrations = renderers.ToArray();
        var duplicate = registrations
            .GroupBy(registration => registration.Key)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Template renderer '{duplicate.Key}' is registered more than once.",
                nameof(renderers));
        }

        _renderers = registrations.ToDictionary(registration => registration.Key, registration => registration.Value);
        RegisteredTemplateIds = _renderers.Keys
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<TemplateId> RegisteredTemplateIds { get; }

    public static TemplateRegistry CreateDefault() => new(
    [
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("object-spotlight"),
            ObjectSpotlightRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("picture-match"),
            PictureMatchRenderer.Render),
        new KeyValuePair<TemplateId, TemplateRendererFactory>(
            new TemplateId("word-order-train"),
            WordOrderTrainRenderer.Render),
    ]);

    public Control Render(
        TemplateId templateId,
        ResolvedTemplateParameters parameters,
        LanguageCode instructionLanguage,
        bool shouldReduceMotion,
        Action<TemplateOutcome> reportOutcome)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(reportOutcome);

        if (!_renderers.TryGetValue(templateId, out var renderer))
        {
            throw new KeyNotFoundException($"Template renderer '{templateId}' is not registered.");
        }

        return renderer(parameters, instructionLanguage, shouldReduceMotion, reportOutcome);
    }
}
