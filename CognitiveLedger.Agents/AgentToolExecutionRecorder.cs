namespace CognitiveLedger.Agents;

public sealed class AgentToolExecutionRecorder : IAgentToolExecutionRecorder
{
    private readonly List<AgentToolExecution> _executions = [];

    public IReadOnlyList<AgentToolExecution> Executions => _executions;

    public void Reset() => _executions.Clear();

    public void Record(AgentToolExecution execution)
    {
        ArgumentNullException.ThrowIfNull(execution);
        _executions.Add(execution);
    }
}
