using iText.Kernel.Pdf;

namespace CognitiveLedger.Parser.PDF;

public static class PdfPageSplitter
{
    public static IReadOnlyList<byte[]> SplitPages(byte[] pdfData)
    {
        ArgumentNullException.ThrowIfNull(pdfData);

        if (pdfData.Length == 0)
        {
            throw new ArgumentException("PDF data cannot be empty.", nameof(pdfData));
        }

        using var inputStream = new MemoryStream(pdfData, writable: false);
        using var sourceDocument = new PdfDocument(new PdfReader(inputStream));
        var pages = new List<byte[]>(sourceDocument.GetNumberOfPages());

        for (var pageNumber = 1; pageNumber <= sourceDocument.GetNumberOfPages(); pageNumber++)
        {
            using var outputStream = new MemoryStream();
            using (var pageDocument = new PdfDocument(new PdfWriter(outputStream)))
            {
                sourceDocument.CopyPagesTo(pageNumber, pageNumber, pageDocument);
            }
            pages.Add(outputStream.ToArray());
        }
        return pages;
    }
}