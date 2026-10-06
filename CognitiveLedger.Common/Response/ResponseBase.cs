namespace CognitiveLedger.Common.Response;

public abstract class ResponseBase
{
    public required ResponseStatus Status { get; set; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }
}