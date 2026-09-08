using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;

namespace CognitiveLedger.Parser.PDF.Interfaces;

public interface IPdfRasterizer
{
    RasterizePdfResponse RasterizePdf(RasterizePdfRequest request);
}
