using CognitiveLedger.Common;
using CognitiveLedger.Parser.PDF;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Exceptions;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.Testing.Import;

public class PdfPiiSanitizerTests
{
    [Test]
    public async Task SanitizePiiAsync_ReplacementTokenIsPresent_Succeeds()
    {
        var sanitizer = CreateSanitizer("Customer: CL_PERSON_0001");

        var response = await sanitizer.SanitizePiiAsync(CreateRequest());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.RasterizedPdfData, Is.Not.Empty);
            Assert.That(response.SanitizedPageText, Has.Count.EqualTo(1));
            Assert.That(response.SanitizedPageText[0].PageNumber, Is.EqualTo(1));
            Assert.That(response.SanitizedPageText[0].Text, Does.Contain("CL_PERSON_0001"));
            Assert.That(response.SanitizedPageText[0].Text, Does.Not.Contain("Dan Maroff"));
        }
    }

    [Test]
    public void SanitizePiiAsync_ReplacementTokenIsMissing_ThrowsException()
    {
        var sanitizer = CreateSanitizer("Customer:");

        var exception = Assert.ThrowsAsync<PdfPiiRedactionException>(async () =>
            await sanitizer.SanitizePiiAsync(CreateRequest()));

        Assert.That(exception!.Message, Does.Contain("replacement token"));
    }

    private static PdfPiiSanitizer CreateSanitizer(string verificationText)
    {
        return new PdfPiiSanitizer(
            new SequenceTextExtractor(
                "Customer: Dan Maroff",
                verificationText),
            new ProvidedValueDetector(),
            new StubPdfRedactor(),
            new StubPdfRasterizer(),
            new AppLog<PdfPiiSanitizer>(
                NullLogger<PdfPiiSanitizer>.Instance));
    }

    private static SanitizePiiRequest CreateRequest()
    {
        return new SanitizePiiRequest
        {
            UserId = 1,
            PdfData = [1],
            Replacements =
            [
                new PdfTextReplacement
                {
                    OriginalValue = "Dan Maroff",
                    ReplacementValue = "CL_PERSON_0001"
                }
            ]
        };
    }

    private sealed class SequenceTextExtractor : IPdfTextExtractor
    {
        private readonly Queue<string> _responses;

        public SequenceTextExtractor(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public ExtractPdfTextResponse ExtractPdfText(
            ExtractPdfTextRequest request)
        {
            return new ExtractPdfTextResponse
            {
                Pages =
                [
                    new PdfPageText
                    {
                        PageNumber = 1,
                        Text = _responses.Peek()
                    }
                ],
                FullText = _responses.Dequeue(),
                TextItems = []
            };
        }
    }

    private sealed class ProvidedValueDetector : IPiiDetector
    {
        public IReadOnlyCollection<PiiItem> PiiItems => [];

        public DetectPiiResponse DetectPii(DetectPiiRequest request)
        {
            throw new InvalidOperationException(
                "This test expects explicit replacement values.");
        }
    }

    private sealed class StubPdfRedactor : IPdfRedactor
    {
        public RedactPdfResponse RedactPdf(RedactPdfRequest request)
        {
            return new RedactPdfResponse
            {
                PdfData = [2]
            };
        }
    }

    private sealed class StubPdfRasterizer : IPdfRasterizer
    {
        public RasterizePdfResponse RasterizePdf(RasterizePdfRequest request)
        {
            return new RasterizePdfResponse
            {
                PdfData = [3],
                PageCount = 1,
                Dpi = 200
            };
        }
    }
}
