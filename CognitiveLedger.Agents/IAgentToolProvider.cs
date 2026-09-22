using Microsoft.Extensions.AI;

namespace CognitiveLedger.Agents;

public interface IAgentToolProvider
{
    ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        CancellationToken cancellationToken = default);
}
