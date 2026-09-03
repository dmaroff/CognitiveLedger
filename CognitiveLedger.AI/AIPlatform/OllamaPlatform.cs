using System.Net.Http.Json;
using CognitiveLedger.AI.AIPlatform;

namespace CognitiveLedger.AI.AIPlatform;

public class OllamaPlatform : PlatformBase, IAiPlatform
{
    public OllamaPlatform(string host, int port)
        : base(host, port)
    {
    }
    
    public async Task<GenerateResponse> GenerateAsync(
        GenerateRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpResponse = await HttpClient.PostAsJsonAsync(
                "/api/generate",
                request,
                cancellationToken);

            httpResponse.EnsureSuccessStatusCode();
            var response = await httpResponse.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken);
            if (response == null)
            {
                throw new AiGenerateException("Received null response from Ollama platform");
            }
            return response.Response == string.Empty
                ? throw new AiGenerateException("Failed to retrieve response from Ollama platform")
                : response;
        }
        catch (OperationCanceledException)
        {
            // Let callers distinguish "timed out" from "actually failed" via their own
            // catch (OperationCanceledException) instead of masking it as an AiGenerateException.
            throw;
        }
        catch (Exception ex)
        {
            throw new AiGenerateException(ex);
        }
    }
}