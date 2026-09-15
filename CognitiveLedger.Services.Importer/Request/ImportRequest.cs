namespace CognitiveLedger.Services.Importer.Request;

// Internal service request. The HTTP controller decodes its base64 payload to FileData.
public sealed class ImportRequest : RequestBase
{
    public ImportFileType FileType { get; init; } = ImportFileType.Unknown;
    public byte[] FileData { get; init; } = [];
    public string FileName { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public StatementType StatementType { get; init; } = StatementType.Unknown;
    public string[] PiiToRedact { get; init; } = [];
}
