using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;

namespace CognitiveLedger.Parser.PDF;

public interface IPiiSanitizer
{
    Task<SanitizePiiResponse> SanitizePiiAsync(
        SanitizePiiRequest request,
        CancellationToken cancellationToken = default);
}