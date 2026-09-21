namespace CognitiveLedger.Data.Models;

public sealed class Status
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public static class StatusCatalog
{
    public const long ProcessingId = 1;
    public const long SuccessId = 2;
    public const long FailedId = 3;
}
