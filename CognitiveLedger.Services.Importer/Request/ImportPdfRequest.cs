using System.ComponentModel.DataAnnotations;

namespace CognitiveLedger.Services.Importer.Request;

public sealed class ImportPdfRequest : RequestBase
{
    [Required]
    public string Base64PdfData { get; set; } = string.Empty;

    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public string BankName { get; set; } = string.Empty;

    public StatementType StatementType { get; set; } = StatementType.Unknown;

    public string[] PiiToRedact { get; set; } = [];
}
