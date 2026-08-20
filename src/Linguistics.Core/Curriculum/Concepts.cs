using System.Text.Json.Serialization;
using Linguistics.Core.Profiles;

namespace Linguistics.Core.Curriculum;

public readonly record struct ConceptId
{
    [JsonConstructor]
    public ConceptId(string value) => Value = CurriculumIdentifier.Normalize(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct TransferMappingId
{
    [JsonConstructor]
    public TransferMappingId(string value) => Value = CurriculumIdentifier.Normalize(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct VersionId
{
    [JsonConstructor]
    public VersionId(string value) => Value = CurriculumIdentifier.Normalize(value, nameof(value));

    public string Value { get; }

    public override string ToString() => Value;
}

internal static class CurriculumIdentifier
{
    public static string Normalize(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 128 ||
            !normalized.All(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_'))
        {
            throw new ArgumentException(
                "Curriculum identifiers may contain lowercase letters, digits, dots, hyphens, and underscores.",
                parameterName);
        }

        return normalized;
    }
}

public enum ConceptType
{
    Lexical,
    Grammatical,
    Phonological,
    Pragmatic,
    Discourse,
    Listening,
    Sociolinguistic,
}

public sealed record ConceptNode(
    ConceptId Id,
    LanguageCode TargetLanguage,
    ConceptType Type,
    string Title,
    string Description,
    string? Cefr,
    IReadOnlyList<ConceptId> Prerequisites,
    IReadOnlyList<string> SuccessCriteria,
    IReadOnlyList<string> ErrorRuleReferences,
    IReadOnlyList<string> TaskTags,
    VersionId ContentVersion);

public sealed class ConceptGraph
{
    private readonly IReadOnlyDictionary<ConceptId, ConceptNode> _nodesById;

    public ConceptGraph(IEnumerable<ConceptNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(nodes);

        var materialized = nodes.ToArray();
        var errors = ValidateNodes(materialized);
        if (errors.Count > 0)
        {
            throw new CurriculumValidationException(errors);
        }

        _nodesById = materialized.ToDictionary(node => node.Id);
        Nodes = materialized.OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<ConceptNode> Nodes { get; }

    public ConceptNode Get(ConceptId id) =>
        _nodesById.TryGetValue(id, out var node)
            ? node
            : throw new KeyNotFoundException($"Concept '{id}' is not in the graph.");

    public bool IsReady(
        ConceptId id,
        IReadOnlyDictionary<ConceptId, ConceptProgressState> progressStates)
    {
        ArgumentNullException.ThrowIfNull(progressStates);

        return Get(id).Prerequisites.All(prerequisite =>
            progressStates.TryGetValue(prerequisite, out var state) &&
            state is ConceptProgressState.ProvisionallyMastered or ConceptProgressState.Mastered);
    }

    private static List<string> ValidateNodes(IReadOnlyList<ConceptNode> nodes)
    {
        var errors = new List<string>();
        var validNodes = nodes.OfType<ConceptNode>().ToArray();
        if (validNodes.Length != nodes.Count)
        {
            errors.Add("A concept node is missing.");
        }

        foreach (var duplicate in validNodes
                     .GroupBy(node => node.Id)
                     .Where(group => group.Count() > 1))
        {
            errors.Add($"Concept ID '{duplicate.Key}' appears more than once.");
        }

        var ids = validNodes.Select(node => node.Id).ToHashSet();
        var byId = validNodes
            .GroupBy(node => node.Id)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single());
        foreach (var node in validNodes)
        {
            ValidateNode(node, errors);
            foreach (var prerequisite in node.Prerequisites ?? [])
            {
                if (!ids.Contains(prerequisite))
                {
                    errors.Add($"Concept '{node.Id}' references missing prerequisite '{prerequisite}'.");
                }
                else if (byId.TryGetValue(prerequisite, out var prerequisiteNode) &&
                         prerequisiteNode.TargetLanguage != node.TargetLanguage)
                {
                    errors.Add(
                        $"Concept '{node.Id}' references prerequisite '{prerequisite}' from another target language.");
                }
            }
        }

        if (errors.Count == 0)
        {
            FindCycles(validNodes, errors);
        }

        return errors;
    }

    private static void ValidateNode(ConceptNode node, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(node.Id.Value))
        {
            errors.Add("A concept ID is missing.");
        }

        if (string.IsNullOrWhiteSpace(node.TargetLanguage.Value))
        {
            errors.Add($"Concept '{node.Id}' has no target language.");
        }

        if (!Enum.IsDefined(node.Type))
        {
            errors.Add($"Concept '{node.Id}' has an invalid type.");
        }

        if (string.IsNullOrWhiteSpace(node.Title) || string.IsNullOrWhiteSpace(node.Description))
        {
            errors.Add($"Concept '{node.Id}' needs a title and description.");
        }

        if (node.Prerequisites is null)
        {
            errors.Add($"Concept '{node.Id}' has no prerequisite collection.");
        }
        else if (node.Prerequisites.Count != node.Prerequisites.Distinct().Count())
        {
            errors.Add($"Concept '{node.Id}' repeats a prerequisite.");
        }

        if (node.SuccessCriteria is null ||
            node.SuccessCriteria.Count == 0 ||
            node.SuccessCriteria.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"Concept '{node.Id}' needs explicit success criteria.");
        }

        if (node.ErrorRuleReferences is null || node.ErrorRuleReferences.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"Concept '{node.Id}' has invalid error-rule references.");
        }

        if (node.TaskTags is null || node.TaskTags.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add($"Concept '{node.Id}' has invalid task tags.");
        }

        if (string.IsNullOrWhiteSpace(node.ContentVersion.Value))
        {
            errors.Add($"Concept '{node.Id}' has no content version.");
        }
    }

    private static void FindCycles(
        IReadOnlyList<ConceptNode> nodes,
        ICollection<string> errors)
    {
        var byId = nodes.ToDictionary(node => node.Id);
        var states = new Dictionary<ConceptId, VisitState>();

        foreach (var node in nodes.OrderBy(node => node.Id.Value, StringComparer.Ordinal))
        {
            Visit(node.Id, byId, states, errors);
        }
    }

    private static void Visit(
        ConceptId id,
        IReadOnlyDictionary<ConceptId, ConceptNode> nodes,
        IDictionary<ConceptId, VisitState> states,
        ICollection<string> errors)
    {
        if (states.TryGetValue(id, out var existing))
        {
            if (existing == VisitState.Visiting)
            {
                errors.Add($"The concept graph contains a cycle involving '{id}'.");
            }

            return;
        }

        states[id] = VisitState.Visiting;
        foreach (var prerequisite in nodes[id].Prerequisites)
        {
            Visit(prerequisite, nodes, states, errors);
        }

        states[id] = VisitState.Visited;
    }

    private enum VisitState
    {
        Visiting,
        Visited,
    }
}

public sealed class CurriculumValidationException : Exception
{
    public CurriculumValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }
}
