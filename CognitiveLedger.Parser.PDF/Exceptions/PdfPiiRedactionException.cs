namespace CognitiveLedger.Parser.PDF.Exceptions;

public sealed class PdfPiiRedactionException : Exception
{
    public PdfPiiRedactionException(string message)
        : base(message)
    {
    }

    public PdfPiiRedactionException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}