using System.Net;
using System.Text;
using System.Text.Json;
using Linguistics.App.LocalAI;
using Linguistics.Core.Providers;

namespace Linguistics.App.Tests;

[TestClass]
public sealed class OllamaProviderTests
{
    [TestMethod]
    [DataRow("https://localhost:11434/")]
    [DataRow("http://example.com:11434/")]
    [DataRow("http://localhost:11434/api/")]
    [DataRow("http://user@localhost:11434/")]
    public void RemoteOrAmbiguousEndpointsAreRejected(string endpoint)
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(JsonResponse("{}"))));

        Assert.ThrowsExactly<ArgumentException>(() =>
            new OllamaProvider(client, new Uri(endpoint)));
    }

    [TestMethod]
    public async Task ServiceInspectionNormalizesLocalModelsAndMarksCloudAliases()
    {
        using var client = new HttpClient(new StubHandler((request, _) =>
        {
            var json = request.RequestUri?.AbsolutePath switch
            {
                "/api/version" => "{\"version\":\"0.12.6\"}",
                "/api/tags" => """
                    {
                      "models": [
                        {
                          "name": "zeta:4b",
                          "modified_at": "2026-08-20T10:00:00Z",
                          "size": 2000000000,
                          "digest": "sha256:zeta",
                          "details": {"format":"gguf","family":"zeta","parameter_size":"4B","quantization_level":"Q4_K_M"}
                        },
                        {
                          "name": "alpha:8b-cloud",
                          "size": 0,
                          "digest": "sha256:cloud",
                          "details": {"format":"","family":"alpha","parameter_size":"8B","quantization_level":""}
                        }
                      ]
                    }
                    """,
                _ => throw new AssertFailedException("Unexpected Ollama endpoint."),
            };
            return Task.FromResult(JsonResponse(json));
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var snapshot = await provider.InspectServiceAsync();

        Assert.AreEqual(LocalModelServiceStatus.Available, snapshot.Status);
        Assert.AreEqual("0.12.6", snapshot.Version);
        Assert.HasCount(2, snapshot.Models);
        Assert.AreEqual("alpha:8b-cloud", snapshot.Models[0].Name);
        Assert.IsTrue(snapshot.Models[0].IsCloudAlias);
        Assert.IsFalse(snapshot.Models[1].IsCloudAlias);
        StringAssert.Contains(snapshot.Message, "cloud alias");
    }

    [TestMethod]
    public async Task ModelInspectionExposesReportedLicenseWithoutMakingARecommendation()
    {
        using var client = new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            StringAssert.Contains(body, "fixture:local");
            return JsonResponse("{\"license\":[\"License A\",\"Notice B\"],\"capabilities\":[\"completion\"]}");
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var details = await provider.InspectModelAsync("fixture:local");

        Assert.AreEqual(LocalModelServiceStatus.Available, details.Status);
        StringAssert.Contains(details.LicenseText, "License A");
        StringAssert.Contains(details.Message, "requires project review");
        CollectionAssert.AreEqual(new[] { "completion" }, details.Capabilities.ToArray());
    }

    [TestMethod]
    public async Task CloudModelInspectionIsBlockedWithoutCallingOllama()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(JsonResponse("{}"));
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var details = await provider.InspectModelAsync("example:70b-cloud");

        Assert.AreEqual(LocalModelServiceStatus.InvalidResponse, details.Status);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task ValidBoundedChatRequestReturnsAcceptedProposal()
    {
        string? requestBody = null;
        using var client = new HttpClient(new StubHandler(async (request, cancellationToken) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            return ChatResponse(ValidProposalJson());
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);
        var request = CreateRequest();

        var result = await provider.GenerateDialogueAsync(request);

        Assert.AreEqual(LanguageModelResultStatus.Accepted, result.Status);
        Assert.IsNotNull(result.Proposal);
        Assert.AreEqual("order-confirmed", result.Proposal.TaskStateProposal);
        Assert.AreEqual("accepted", result.Diagnostic.ValidationResult);
        Assert.IsNotNull(requestBody);
        using var document = JsonDocument.Parse(requestBody);
        Assert.IsFalse(document.RootElement.GetProperty("stream").GetBoolean());
        Assert.IsFalse(document.RootElement.GetProperty("think").GetBoolean());
        Assert.AreEqual(0, document.RootElement.GetProperty("options").GetProperty("temperature").GetInt32());
        StringAssert.Contains(requestBody, DialogueProposalValidator.SchemaVersion);
        Assert.IsFalse(requestBody.Contains("teacherNotes", StringComparison.Ordinal));
    }

    [TestMethod]
    [DataRow("not json", "proposal.json")]
    [DataRow("{\"npcResponse\":\"Invented\",\"intent\":\"accept-order\",\"taskStateProposal\":\"order-confirmed\",\"usedVocabulary\":[]}", "proposal.npc-response")]
    [DataRow("{\"npcResponse\":\"Gern. Einen Kaffee.\",\"intent\":\"accept-order\",\"taskStateProposal\":\"success\",\"usedVocabulary\":[]}", "proposal.transition")]
    public async Task InvalidModelProposalUsesScriptedFallback(string proposal, string expectedCode)
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(ChatResponse(proposal))));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var result = await provider.GenerateDialogueAsync(CreateRequest());

        Assert.AreEqual(LanguageModelResultStatus.InvalidResponse, result.Status);
        Assert.IsNull(result.Proposal);
        Assert.AreEqual("Scripted café reply.", result.ScriptedFallback);
        Assert.AreEqual(expectedCode, result.Diagnostic.ValidationResult);
    }

    [TestMethod]
    public async Task MissingSelectionUsesFallbackWithoutCallingOllama()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(ChatResponse(ValidProposalJson()));
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var result = await provider.GenerateDialogueAsync(CreateRequest() with { SelectedModel = null });

        Assert.AreEqual(LanguageModelResultStatus.NoModelSelected, result.Status);
        Assert.AreEqual(0, calls);
    }

    [TestMethod]
    public async Task MalformedChatEnvelopeFailsClosed()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            Task.FromResult(JsonResponse("{\"done\":false,\"message\":{}}"))));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);

        var result = await provider.GenerateDialogueAsync(CreateRequest());

        Assert.AreEqual(LanguageModelResultStatus.InvalidResponse, result.Status);
        Assert.IsNull(result.Proposal);
        Assert.AreEqual("envelope.invalid", result.Diagnostic.ValidationResult);
    }

    [TestMethod]
    public async Task TimeoutAndCancellationRemainDistinctAndNeverReturnAProposal()
    {
        using var client = new HttpClient(new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return ChatResponse(ValidProposalJson());
        }));
        using var provider = new OllamaProvider(
            client,
            OllamaProvider.DefaultEndpoint,
            TimeSpan.FromMilliseconds(30));

        var timedOut = await provider.GenerateDialogueAsync(CreateRequest());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = await provider.GenerateDialogueAsync(CreateRequest(), cancellation.Token);

        Assert.AreEqual(LanguageModelResultStatus.TimedOut, timedOut.Status);
        Assert.IsNull(timedOut.Proposal);
        Assert.AreEqual(LanguageModelResultStatus.Cancelled, cancelled.Status);
        Assert.IsNull(cancelled.Proposal);
    }

    [TestMethod]
    public async Task OlderResponseForTheSameSessionIsDiscardedAsStale()
    {
        var callCount = 0;
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new StubHandler(async (_, cancellationToken) =>
        {
            if (Interlocked.Increment(ref callCount) == 1)
            {
                firstEntered.SetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
            }

            return ChatResponse(ValidProposalJson());
        }));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);
        var sessionId = Guid.NewGuid();
        var firstTask = provider.GenerateDialogueAsync(CreateRequest() with { SessionId = sessionId });
        await firstEntered.Task;
        var second = await provider.GenerateDialogueAsync(CreateRequest() with { SessionId = sessionId });
        releaseFirst.SetResult();
        var first = await firstTask;

        Assert.AreEqual(LanguageModelResultStatus.Accepted, second.Status);
        Assert.AreEqual(LanguageModelResultStatus.Stale, first.Status);
        Assert.IsNull(first.Proposal);
    }

    [TestMethod]
    public async Task UnavailableServiceKeepsFallbackAndSafeDiagnostics()
    {
        using var client = new HttpClient(new StubHandler((_, _) =>
            throw new HttpRequestException("fixture endpoint unavailable")));
        using var provider = new OllamaProvider(client, OllamaProvider.DefaultEndpoint);
        var request = CreateRequest();

        var result = await provider.GenerateDialogueAsync(request);

        Assert.AreEqual(LanguageModelResultStatus.Unavailable, result.Status);
        Assert.AreEqual(request.RequestId, result.Diagnostic.RequestId);
        Assert.AreEqual("fixture:local", result.Diagnostic.Model);
        Assert.IsFalse(result.Message.Contains(request.LearnerUtterance, StringComparison.Ordinal));
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
            "Scripted café reply.");

    private static string ValidProposalJson() =>
        """
        {
          "npcResponse": "Gern. Einen Kaffee.",
          "intent": "accept-order",
          "taskStateProposal": "order-confirmed",
          "usedVocabulary": ["de.lexeme.kaffee"]
        }
        """;

    private static HttpResponseMessage ChatResponse(string proposal) =>
        JsonResponse(JsonSerializer.Serialize(new
        {
            done = true,
            message = new
            {
                role = "assistant",
                content = proposal,
            },
        }));

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            response(request, cancellationToken);
    }
}
