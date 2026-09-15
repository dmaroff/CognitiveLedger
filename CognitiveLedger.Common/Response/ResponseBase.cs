namespace CognitiveLedger.Common.Response;

public abstract class ResponseBase
{
    public ResponseStatus Status { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }
}