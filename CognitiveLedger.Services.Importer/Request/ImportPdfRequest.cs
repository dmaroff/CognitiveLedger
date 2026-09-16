using System.ComponentModel.DataAnnotations;
using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Services.Importer.Request;

public sealed class ImportPdfRequest : RequestBase
{
    [Required]
    public string Base64PdfData { get; init; } = string.Empty;

    [Required]
    public string FileName { get; init; } = string.Empty;

    [Required]
    public string BankName { get; init; } = string.Empty;

    public StatementType StatementType { get; init; } = StatementType.Unknown;

    public string[] PiiToRedact { get; init; } = [];
}
