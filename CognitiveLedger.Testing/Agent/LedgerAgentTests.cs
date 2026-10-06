using System.Runtime.CompilerServices;
using CognitiveLedger.Agents;
using CognitiveLedger.Common;
using CognitiveLedger.Privacy;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.Testing.Agent;

public class LedgerAgentTests
{
    [Test]
    public async Task RunAsync_MultipleAssistantMessages_ReturnsOnlyFinalMessage()
    {
        var chatClient = new MultiMessageChatClient();
        var agent = new LedgerAgent(
            new AppLog<LedgerAgent>(NullLogger<LedgerAgent>.Instance),
            chatClient,
            new EmptyToolProvider(),
            new AgentToolExecutionRecorder(),
            new TokenMap());

        var response = await agent.RunAsync(new AgentRequest
        {
            UserId = 1,
            Message = "Answer using the available tools."
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Answer, Is.EqualTo("The final answer."));
            Assert.That(chatClient.Options, Is.Not.Null);
            Assert.That(chatClient.Options!.MaxOutputTokens, Is.EqualTo(1500));
        }
    }

    private sealed class EmptyToolProvider : IAgentToolProvider
    {
        public ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlyList<AITool>>([]);
    }

    private sealed class MultiMessageChatClient : IChatClient
    {
        public ChatOptions? Options { get; private set; }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult(new ChatResponse(
            [
                new ChatMessage(ChatRole.Assistant, "I'll retrieve the information."),
                new ChatMessage(ChatRole.Assistant, "The final answer.")
            ]));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType.IsInstanceOfType(this) ? this : null;

        public void Dispose()
        {
        }
    }
}
