namespace CognitiveLedger.AI.AIPlatform;

public interface IAiPlatform
{
    Task<GenerateResponse> GenerateAsync(
        GenerateRequest request,
        CancellationToken cancellationToken);
}