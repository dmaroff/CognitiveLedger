namespace CognitiveLedger.Agents;

public interface ILedgerAgent
{
    Task<AgentResponse> RunAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<AgentEvent> StreamAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default);
}
