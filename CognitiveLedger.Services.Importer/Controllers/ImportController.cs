using CognitiveLedger.Common.Response;
using CognitiveLedger.Data.Repositories;
using Microsoft.AspNetCore.Mvc;
using CognitiveLedger.Services.Importer.Request;
using CognitiveLedger.Services.Importer.Response;
using CognitiveLedger.Services.Importer.Services;
using StatusCatalog = CognitiveLedger.Data.Models.StatusCatalog;

namespace CognitiveLedger.Services.Importer.Controllers;

[ApiController]
[Route("api/parse")]
public sealed class ImportController(
    IImportService importService,
    IStatementProcessingRepository processingRepository) : ControllerBase
{
    [HttpPost("pdf")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ImportPdfResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ImportPdfResponse), StatusCodes.Status501NotImplemented)]
    public async Task<ActionResult<ImportPdfResponse>> ImportPdf(
        [FromBody] ImportPdfRequest request,
        CancellationToken cancellationToken)
    {
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
        
        try
        {
            _ = Convert.FromBase64String(request.Base64PdfData);
        }
        catch (FormatException)
        {
            ModelState.AddModelError(nameof(request.Base64PdfData), "PDF data must be valid base64.");
            return ValidationProblem(ModelState);
        }

        var importRequest = (ImportRequest)request;
        var result = await importService.ImportAsync(importRequest, cancellationToken);

        var response = new ImportPdfResponse
        {
            Existing = result.Existing,
            ProcessingAuditId = result.ProcessingAuditId,
            StatementId = result.StatementId,
            Status = result.Status,
            ErrorCode = result.ErrorCode,
            ErrorMessage = result.ErrorMessage
        };

        return result.Status == ResponseStatus.Success
            ? Ok(response)
            : StatusCode(StatusCodes.Status501NotImplemented, response);
    }

    [HttpGet("status/{processingAuditId:long}")]
    [ProducesResponseType(typeof(ImportStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportStatusResponse>> GetImportStatus(
        long processingAuditId,
        [FromQuery] long userId,
        CancellationToken cancellationToken)
    {
        if (processingAuditId <= 0 || userId <= 0)
        {
            return BadRequest();
        }

        var audit = await processingRepository.FindAsync(
            processingAuditId,
            userId,
            cancellationToken);

        if (audit is null)
        {
            return NotFound();
        }

        return Ok(new ImportStatusResponse
        {
            ProcessingAuditId = audit.Id,
            Status = audit.StatusId switch
            {
                StatusCatalog.ProcessingId => ImportProcessingStatus.Processing,
                StatusCatalog.SuccessId => ImportProcessingStatus.Succeeded,
                StatusCatalog.FailedId => ImportProcessingStatus.Failed,
                _ => ImportProcessingStatus.Failed
            },
            StatementId = audit.StatementId,
            ExtractedTransactionCount = audit.ExtractedTransactionCount,
            ErrorMessage = audit.ErrorMessage
        });
    }
}
