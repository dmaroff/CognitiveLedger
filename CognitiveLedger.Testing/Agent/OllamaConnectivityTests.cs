namespace CognitiveLedger.Testing.Agent;

using Microsoft.Extensions.AI;
using OllamaSharp;

public class OllamaConnectivityTests
{
    [Test]
    [Explicit("Requires the Ollama Docker container and qwen3:8b.")]
    [Category("Integration")]
    public async Task ModelCanGenerateResponse()
    {
        IChatClient client = new OllamaApiClient(
            new Uri("http://127.0.0.1:11555"),
            "qwen3:8b");

        var response = await client.GetResponseAsync(
            [
                new ChatMessage(
                    ChatRole.User,
                    "Reply with a short confirmation that you are available.")
            ],
            new ChatOptions
            {
                MaxOutputTokens = 4,
                Reasoning = new ReasoningOptions
                {
                    Effort = ReasoningEffort.None,
                    Output = ReasoningOutput.None
                }
            });

        Assert.That(response.Text, Is.Not.Null.And.Not.Empty);
    }
}