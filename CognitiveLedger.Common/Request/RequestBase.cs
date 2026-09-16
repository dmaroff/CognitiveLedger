namespace CognitiveLedger.Common.Request;

public abstract class RequestBase
{
    public Guid RequestId { get; init; } = Guid.NewGuid();
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    
    public required int UserId { get; init; }

    public override string ToString()
    {
        return $"[RequestId={RequestId}, UserId={UserId}]";
    }
}