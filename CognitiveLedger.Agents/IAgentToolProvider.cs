using Microsoft.Extensions.AI;

namespace CognitiveLedger.Agents;

public interface IAgentToolProvider
{
    /// <summary>
    /// Gets the list of tools available for the agent to use.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        CancellationToken cancellationToken = default);
}