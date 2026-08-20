using Linguistics.Core.Providers;

namespace Linguistics.Core.Tests;

[TestClass]
public sealed class LanguageModelContractTests
{
    [TestMethod]
    public void ExactAllowedProposalIsAccepted()
    {
        var result = DialogueProposalValidator.Validate(
            CreateRequest(),
            """
            {
              "npcResponse": "Gern. Einen Kaffee.",
              "intent": "accept-order",
              "taskStateProposal": "order-confirmed",
              "usedVocabulary": ["de.lexeme.kaffee"]
            }
            """);

        Assert.IsTrue(result.IsValid);
        Assert.IsNotNull(result.Proposal);
        Assert.AreEqual("accepted", result.Code);
    }

    [TestMethod]
    [DataRow("not json", "proposal.json")]
    [DataRow("{}", "proposal.json")]
    [DataRow("{\"npcResponse\":\"Invented\",\"intent\":\"accept-order\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[]}", "proposal.npc-response")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"invented\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[]}", "proposal.intent")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"accept-order\",\"taskStateProposal\":\"success\",\"usedVocabulary\":[]}", "proposal.transition")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"accept-order\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[\"unknown\"]}", "proposal.vocabulary")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"accept-order\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[],\"extra\":true}", "proposal.json")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"accept-order\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[]}", "proposal.json")]
    public void InvalidProposalFailsClosed(string json, string expectedCode)
    {
        var result = DialogueProposalValidator.Validate(CreateRequest(), json);

        Assert.IsFalse(result.IsValid);
        Assert.IsNull(result.Proposal);
        Assert.AreEqual(expectedCode, result.Code);
    }

    [TestMethod]
    public void RequestRequiresClosedAllowedValuesAndNoEmptyFacts()
    {
        var invalid = CreateRequest() with { AllowedNextStates = [] };

        Assert.ThrowsExactly<ArgumentException>(() =>
            DialogueProposalValidator.ValidateRequest(invalid));
    }

    private static DialogueGenerationRequest CreateRequest() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "fixture:local",
            "Café server",
            "Confirm one allowed drink order.",
            "awaiting-order",
            ["accept-order"],
            ["order-confirmed"],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["de.lexeme.kaffee"] = "Kaffee",
            },
            ["Gern. Einen Kaffee."],
            ["Kaffee is available."],
            "Ich möchte einen Kaffee, bitte.",
            "Gern. Einen Kaffee.");
}
