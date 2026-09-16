using CognitiveLedger.Common.Request;

namespace CognitiveLedger.Services.Importer.Request;

// Internal service request. The HTTP controller decodes its base64 payload to FileData.
public sealed class ImportRequest : RequestBase
{
    public ImportFileType FileType { get; init; } = ImportFileType.Unknown;
    public byte[] FileData { get; private init; } = [];
    public string FileName { get; private init; } = string.Empty;
    public string SourceName { get; private init; } = string.Empty;
    public StatementType StatementType { get; private init; } = StatementType.Unknown;
    public string[] PiiToRedact { get; private init; } = [];
    
    public static explicit operator ImportRequest(ImportPdfRequest request) 
    { 
        return new ImportRequest 
        { 
            // inherited from RequestBase
            RequestId = request.RequestId,
            CreatedDate = request.CreatedDate,
            UserId = request.UserId,
            
            FileType = ImportFileType.Pdf, 
            FileName = request.FileName, 
            SourceName = request.BankName, 
            StatementType = request.StatementType, 
            FileData = Convert.FromBase64String(request.Base64PdfData), 
            PiiToRedact = request.PiiToRedact 
        }; 
    }
    
    public static explicit operator ImportPdfRequest(ImportRequest request) 
    { 
        return new ImportPdfRequest 
        { 
            // inherited from RequestBase
            RequestId = request.RequestId,
            CreatedDate = request.CreatedDate,
            UserId = request.UserId,
            
            FileName = request.FileName, 
            BankName = request.SourceName, 
            StatementType = request.StatementType, 
            Base64PdfData = Convert.ToBase64String(request.FileData), 
            PiiToRedact = request.PiiToRedact 
        }; 
    }

    public override string ToString()
    {
        return $"ImportRequest: " +
               $"RequestId={RequestId}, " +
               $"CreatedDate={CreatedDate}, " +
               $"UserId={UserId}, " +
               $"FileType={FileType}, " +
               $"FileName={FileName}, " +
               $"SourceName={SourceName}, " +
               $"StatementType={StatementType}, " +
               $"FileDataLength={FileData.Length}, " +
               $"PiiToRedactCount={PiiToRedact.Length}";
    }
}
