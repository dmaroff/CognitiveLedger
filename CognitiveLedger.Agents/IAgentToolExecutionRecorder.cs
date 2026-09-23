namespace CognitiveLedger.Agents;

public interface IAgentToolExecutionRecorder
{
    IReadOnlyList<AgentToolExecution> Executions { get; }

    void Reset();

    void Record(AgentToolExecution execution);
}
