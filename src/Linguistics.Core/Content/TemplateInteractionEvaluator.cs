using System.Text;

namespace Linguistics.Core.Content;

public static class TemplateInteractionEvaluator
{
    public static TemplateOutcome EvaluateAcknowledgement(bool acknowledged) =>
        new(acknowledged ? TemplateOutcomeState.Success : TemplateOutcomeState.Ready);

    public static TemplateOutcome EvaluatePictureMatch(
        IReadOnlyList<TemplateOption> options,
        string answerId,
        string? selectedOptionId) =>
        EvaluateSingleSelection(options, answerId, selectedOptionId);

    public static TemplateOutcome EvaluateSingleSelection(
        IReadOnlyList<TemplateOption> options,
        string answerId,
        string? selectedOptionId)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(answerId);

        var optionIds = options.Select(option => option.Id).ToArray();
        if (optionIds.Any(string.IsNullOrWhiteSpace) ||
            optionIds.Distinct(StringComparer.Ordinal).Count() != optionIds.Length)
        {
            throw new ArgumentException(
                "Selection options must have distinct nonempty IDs.",
                nameof(options));
        }

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

    public static TemplateOutcome EvaluatePairCards(
        IReadOnlyList<TemplateOption> pairs,
        IReadOnlyList<string> selectedCardIds)
    {
        ArgumentNullException.ThrowIfNull(pairs);
        ArgumentNullException.ThrowIfNull(selectedCardIds);

        var pairIds = ValidateOptionIds(pairs, nameof(pairs));
        var selected = selectedCardIds.ToArray();
        var validCardIds = pairIds
            .SelectMany(pairId => new[] { $"word:{pairId}", $"image:{pairId}" })
            .ToHashSet(StringComparer.Ordinal);
        if (selected.Length != 2 ||
            selected.Distinct(StringComparer.Ordinal).Count() != selected.Length ||
            selected.Any(cardId => !validCardIds.Contains(cardId)))
        {
            return new TemplateOutcome(
                TemplateOutcomeState.Uncertain,
                OrderedOptionIds: selected);
        }

        var first = selected[0].Split(':', 2);
        var second = selected[1].Split(':', 2);
        var isMatch = !string.Equals(first[0], second[0], StringComparison.Ordinal) &&
                      string.Equals(first[1], second[1], StringComparison.Ordinal);
        return new TemplateOutcome(
            isMatch ? TemplateOutcomeState.Success : TemplateOutcomeState.Failure,
            OrderedOptionIds: selected);
    }

    public static TemplateOutcome EvaluateSortAssignments(
        IReadOnlyList<TemplateOption> items,
        IReadOnlyList<TemplateOption> baskets,
        IReadOnlyDictionary<string, string> expectedAssignments,
        IReadOnlyDictionary<string, string> selectedAssignments)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(baskets);
        ArgumentNullException.ThrowIfNull(expectedAssignments);
        ArgumentNullException.ThrowIfNull(selectedAssignments);

        var itemIds = ValidateOptionIds(items, nameof(items));
        var basketIds = ValidateOptionIds(baskets, nameof(baskets));
        if (expectedAssignments.Count != itemIds.Length ||
            itemIds.Any(itemId => !expectedAssignments.TryGetValue(itemId, out var basketId) ||
                                  !basketIds.Contains(basketId, StringComparer.Ordinal)))
        {
            throw new ArgumentException(
                "Every sort item must map to an available basket.",
                nameof(expectedAssignments));
        }

        var orderedSelections = itemIds
            .Where(selectedAssignments.ContainsKey)
            .Select(itemId => $"{itemId}:{selectedAssignments[itemId]}")
            .ToArray();
        var hasCompleteValidSelection =
            selectedAssignments.Count == itemIds.Length &&
            selectedAssignments.All(pair =>
                itemIds.Contains(pair.Key, StringComparer.Ordinal) &&
                basketIds.Contains(pair.Value, StringComparer.Ordinal));
        if (!hasCompleteValidSelection)
        {
            return new TemplateOutcome(
                TemplateOutcomeState.Uncertain,
                OrderedOptionIds: orderedSelections);
        }

        var isCorrect = itemIds.All(itemId =>
            string.Equals(
                expectedAssignments[itemId],
                selectedAssignments[itemId],
                StringComparison.Ordinal));
        return new TemplateOutcome(
            isCorrect ? TemplateOutcomeState.Success : TemplateOutcomeState.Failure,
            OrderedOptionIds: orderedSelections);
    }

    public static TemplateOutcome EvaluateMappedPair(
        IReadOnlyList<TemplateOption> leftOptions,
        IReadOnlyList<TemplateOption> rightOptions,
        IReadOnlyDictionary<string, string> expectedPairs,
        string? selectedLeftId,
        string? selectedRightId)
    {
        ArgumentNullException.ThrowIfNull(leftOptions);
        ArgumentNullException.ThrowIfNull(rightOptions);
        ArgumentNullException.ThrowIfNull(expectedPairs);

        var leftIds = ValidateOptionIds(leftOptions, nameof(leftOptions));
        var rightIds = ValidateOptionIds(rightOptions, nameof(rightOptions));
        if (expectedPairs.Count != leftIds.Length ||
            leftIds.Any(id => !expectedPairs.ContainsKey(id)) ||
            expectedPairs.Values.Any(id => !rightIds.Contains(id, StringComparer.Ordinal)))
        {
            throw new ArgumentException(
                "Expected pairs must map every left option to a declared right option.",
                nameof(expectedPairs));
        }

        if (selectedLeftId is null || selectedRightId is null)
        {
            return new TemplateOutcome(TemplateOutcomeState.Uncertain);
        }

        if (!leftIds.Contains(selectedLeftId, StringComparer.Ordinal) ||
            !rightIds.Contains(selectedRightId, StringComparer.Ordinal))
        {
            throw new ArgumentException("A selected pair ID is not declared by its option list.");
        }

        return new TemplateOutcome(
            string.Equals(
                expectedPairs[selectedLeftId],
                selectedRightId,
                StringComparison.Ordinal)
                ? TemplateOutcomeState.Success
                : TemplateOutcomeState.Failure,
            OrderedOptionIds: [selectedLeftId, selectedRightId]);
    }

    public static TemplateOutcome EvaluateSelectionPair(
        IReadOnlyList<TemplateOption> leftOptions,
        IReadOnlyList<TemplateOption> rightOptions,
        string expectedLeftId,
        string expectedRightId,
        string? selectedLeftId,
        string? selectedRightId)
    {
        ArgumentNullException.ThrowIfNull(leftOptions);
        ArgumentNullException.ThrowIfNull(rightOptions);

        var leftIds = ValidateOptionIds(leftOptions, nameof(leftOptions));
        var rightIds = ValidateOptionIds(rightOptions, nameof(rightOptions));
        if (!leftIds.Contains(expectedLeftId, StringComparer.Ordinal) ||
            !rightIds.Contains(expectedRightId, StringComparer.Ordinal))
        {
            throw new ArgumentException("An expected pair ID is not declared by its option list.");
        }

        if (selectedLeftId is null || selectedRightId is null)
        {
            return new TemplateOutcome(TemplateOutcomeState.Uncertain);
        }

        if (!leftIds.Contains(selectedLeftId, StringComparer.Ordinal) ||
            !rightIds.Contains(selectedRightId, StringComparer.Ordinal))
        {
            throw new ArgumentException("A selected pair ID is not declared by its option list.");
        }

        return new TemplateOutcome(
            string.Equals(expectedLeftId, selectedLeftId, StringComparison.Ordinal) &&
            string.Equals(expectedRightId, selectedRightId, StringComparison.Ordinal)
                ? TemplateOutcomeState.Success
                : TemplateOutcomeState.Failure,
            OrderedOptionIds: [selectedLeftId, selectedRightId]);
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

    public static TemplateOutcome EvaluateDictation(
        IReadOnlyList<TemplateOption> acceptedAnswers,
        string? response)
    {
        ArgumentNullException.ThrowIfNull(acceptedAnswers);

        ValidateOptionIds(acceptedAnswers, nameof(acceptedAnswers));
        var normalizedAnswers = acceptedAnswers
            .Select(answer => (answer.Id, Text: NormalizeDictation(answer.Label)))
            .ToArray();
        if (normalizedAnswers.Any(answer => answer.Text.Length == 0) ||
            normalizedAnswers.Select(answer => answer.Text)
                .Distinct(StringComparer.Ordinal)
                .Count() != normalizedAnswers.Length)
        {
            throw new ArgumentException(
                "Accepted dictation answers must have distinct nonempty normalized text.",
                nameof(acceptedAnswers));
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            return new TemplateOutcome(TemplateOutcomeState.Uncertain);
        }

        var normalizedResponse = NormalizeDictation(response);
        var match = normalizedAnswers.FirstOrDefault(answer =>
            string.Equals(answer.Text, normalizedResponse, StringComparison.Ordinal));
        return match == default
            ? new TemplateOutcome(TemplateOutcomeState.Failure)
            : new TemplateOutcome(TemplateOutcomeState.Success, match.Id);
    }

    private static string[] ValidateOptionIds(
        IReadOnlyList<TemplateOption> options,
        string parameterName)
    {
        var optionIds = options.Select(option => option.Id).ToArray();
        if (optionIds.Length == 0 ||
            optionIds.Any(string.IsNullOrWhiteSpace) ||
            optionIds.Distinct(StringComparer.Ordinal).Count() != optionIds.Length)
        {
            throw new ArgumentException(
                "Options must have distinct nonempty IDs.",
                parameterName);
        }

        return optionIds;
    }

    private static string NormalizeDictation(string value)
    {
        var withoutTerminalPunctuation = value
            .Normalize(NormalizationForm.FormKC)
            .Trim()
            .TrimEnd('.', '!', '?');
        return string.Join(
                ' ',
                withoutTerminalPunctuation.Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }
}
