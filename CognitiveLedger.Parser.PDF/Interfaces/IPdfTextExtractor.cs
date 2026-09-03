using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;

namespace CognitiveLedger.Parser.PDF.Interfaces;

public interface IPdfTextExtractor
{
    // /// <summary>
    // /// Opens the PDF at <paramref name="filePath"/> and returns the text of every page,
    // /// in page order.
    // /// </summary>
    // string ExtractText(string filePath);
    //
    // /// <summary>
    // /// Opens the PDF from <paramref name="stream"/> and returns the text of every page,
    // /// in page order.
    // /// </summary>
    // string ExtractText(Stream stream);
    
    public ExtractPdfTextResponse ExtractPdfText( ExtractPdfTextRequest request);
}
