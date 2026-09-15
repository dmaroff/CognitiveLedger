using CognitiveLedger.Common.Response;
using Microsoft.AspNetCore.Mvc;
using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;
using CognitiveLedger.Services.Importer.Services;

namespace CognitiveLedger.Services.Importer.Controllers;

[ApiController]
[Route("api/parse")]
public sealed class ImportController(IImportService importService) : ControllerBase
{
    [HttpPost("pdf")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ImportPdfResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportPdfResponse), StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<ImportPdfResponse>> ImportPdf(
        [FromBody] ImportPdfRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BankName == string.Empty)
        {
            ModelState.AddModelError(nameof(request.BankName), "A non-empty bank name is required.");
            return ValidationProblem(ModelState);
        }
        if (request.FileName == string.Empty)
        {
            ModelState.AddModelError(nameof(request.FileName), "A non-empty file name is required.");
            return ValidationProblem(ModelState);
        }
        if (request.Base64PdfData == string.Empty)
        {
            ModelState.AddModelError(nameof(request.Base64PdfData), "Base64 PDF data is required.");
            return ValidationProblem(ModelState);
        }
        if (request.StatementType == StatementType.Unknown)
        {
            ModelState.AddModelError(nameof(request.StatementType), "A valid statement type is required.");
            return ValidationProblem(ModelState);
        }

        byte[] pdfData;
        try
        {
            pdfData = Convert.FromBase64String(request.Base64PdfData);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(nameof(request.Base64PdfData), "PDF data must be valid base64.");
            return ValidationProblem(ModelState);
        }

        var result = await importService.ImportAsync(new ImportRequest
        {
            FileType = ImportFileType.Pdf,
            FileData = pdfData,
            FileName = request.FileName,
            SourceName = request.BankName,
            StatementType = request.StatementType,
            PiiToRedact = request.PiiToRedact
        }, cancellationToken);

        var response = new ImportPdfResponse
        {
            StatementId = result.StatementId,
            Status = result.Status,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };

        return result.Status == ResponseStatus.Success
            ? Ok(response)
            : StatusCode(StatusCodes.Status501NotImplemented, response);
    }
}
