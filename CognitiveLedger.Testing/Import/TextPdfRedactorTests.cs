using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Types;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;


namespace CognitiveLedger.Testing.Import;

public class TextPdfRedactorTests
{
    [Test]
    public void RedactPdf_WithReplacement_RemovesOriginalAndDrawsToken()
    {
        var originalValue = "Dan Maroff";
        var replacementValue = "CL_PERSON_0001";
        var inputPdf = CreatePdf("Customer: " + originalValue);
        var logger = new NoOpAppLog<TextPdfRedactor>();
        var redactor = new TextPdfRedactor(logger);

        var response = redactor.RedactPdf(new RedactPdfRequest
        {
            UserId = 1,
            PdfData = inputPdf,
            PiiItems =
            [
                new PiiItem
                {
                    Type = PiiType.Name,
                    Value = originalValue,
                    PageNumber = 1,
                    Bounds = []
                }
            ],
            Replacements =
            [
                new PdfTextReplacement
                {
                    OriginalValue = originalValue,
                    ReplacementValue = replacementValue
                }
            ]
        });

        var outputText = ExtractText(response.PdfData);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outputText, Does.Not.Contain(originalValue).IgnoreCase);
            Assert.That(outputText, Does.Contain(replacementValue));
        }
    }

    private static byte[] CreatePdf(string text)
    {
        using var outputStream = new MemoryStream();

        using (var pdfDocument = new PdfDocument(new PdfWriter(outputStream)))
        {
            var page = pdfDocument.AddNewPage();
            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var canvas = new PdfCanvas(page);

            canvas.BeginText();
            canvas.SetFontAndSize(font, 12);
            canvas.MoveText(72, 720);
            canvas.ShowText(text);
            canvas.EndText();
        }

        return outputStream.ToArray();
    }

    private static string ExtractText(byte[] pdfData)
    {
        using var inputStream = new MemoryStream(pdfData, writable: false);
        using var pdfDocument = new PdfDocument(new PdfReader(inputStream));

        return PdfTextExtractor.GetTextFromPage(pdfDocument.GetFirstPage());
    }
}