namespace Linguistics.Core.Content;

public static class TemplateInteractionEvaluator
{
    public static TemplateOutcome EvaluateAcknowledgement(bool acknowledged) =>
        new(acknowledged ? TemplateOutcomeState.Success : TemplateOutcomeState.Ready);

    public static TemplateOutcome EvaluatePictureMatch(
        IReadOnlyList<TemplateOption> options,
        string answerId,
        string? selectedOptionId)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(answerId);

        var optionIds = options.Select(option => option.Id).ToArray();
        if (!optionIds.Contains(answerId, StringComparer.Ordinal))
        {
            throw new ArgumentException("The answer must name an available option.", nameof(answerId));
        }

        if (string.IsNullOrWhiteSpace(selectedOptionId) ||
            !optionIds.Contains(selectedOptionId, StringComparer.Ordinal))
        {
            return new TemplateOutcome(TemplateOutcomeState.Uncertain, selectedOptionId);
        }

        return new TemplateOutcome(
            string.Equals(selectedOptionId, answerId, StringComparison.Ordinal)
                ? TemplateOutcomeState.Success
                : TemplateOutcomeState.Failure,
            selectedOptionId);
    }

    public static TemplateOutcome EvaluateWordOrder(
        IReadOnlyList<TemplateOption> expectedOptions,
        IReadOnlyList<string> orderedOptionIds)
    {
        ArgumentNullException.ThrowIfNull(expectedOptions);
        ArgumentNullException.ThrowIfNull(orderedOptionIds);

        var expectedIds = expectedOptions.Select(option => option.Id).ToArray();
        if (expectedIds.Count(id => !string.IsNullOrWhiteSpace(id)) != expectedIds.Length ||
            expectedIds.Distinct(StringComparer.Ordinal).Count() != expectedIds.Length)
        {
            throw new ArgumentException(
                "Expected word-order options must have distinct nonempty IDs.",
                nameof(expectedOptions));
        }

        var selectedIds = orderedOptionIds.ToArray();
        var hasEveryExpectedOption =
            selectedIds.Length == expectedIds.Length &&
            selectedIds.Distinct(StringComparer.Ordinal).Count() == selectedIds.Length &&
            selectedIds.All(id => expectedIds.Contains(id, StringComparer.Ordinal));
        if (!hasEveryExpectedOption)
        {
            return new TemplateOutcome(
                TemplateOutcomeState.Uncertain,
                OrderedOptionIds: selectedIds);
        }

        return new TemplateOutcome(
            selectedIds.SequenceEqual(expectedIds, StringComparer.Ordinal)
                ? TemplateOutcomeState.Success
                : TemplateOutcomeState.Failure,
            OrderedOptionIds: selectedIds);
    }
}
