using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Agents;

public sealed class AgentRequest : RequestBase
{
    public required string Message { get; init; }
    public string? ConversationId { get; init; }
}