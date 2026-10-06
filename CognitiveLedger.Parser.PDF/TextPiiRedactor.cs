using System.Text.RegularExpressions;
using CognitiveLedger.Common;
using CognitiveLedger.Parser.PDF.Dtos;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.PdfCleanup;
using iText.PdfCleanup.Autosweep;


namespace CognitiveLedger.Parser.PDF;

public sealed class TextPdfRedactor : IPdfRedactor
{
    private readonly IAppLog<TextPdfRedactor> _logger;
    private readonly IPdfRasterizer? _pdfRasterizer;

    public TextPdfRedactor(
        IAppLog<TextPdfRedactor> logger,
        IPdfRasterizer? pdfRasterizer = null)
    {
        _logger = logger;
        _pdfRasterizer = pdfRasterizer;
    }

    public RedactPdfResponse RedactPdf(RedactPdfRequest request)
    {
        _logger.LogMethodStart();
        ArgumentNullException.ThrowIfNull(request);

        if (request.PdfData.Length == 0)
        {
            throw new ArgumentException("PDF data cannot be empty.", nameof(request));
        }

        var strategy = new CompositeCleanupStrategy();

        foreach (var piiItem in request.PiiItems)
        {
            if (string.IsNullOrWhiteSpace(piiItem.Value))
            {
                continue;
            }
            var escapedValue = Regex.Escape(piiItem.Value);
            strategy.Add(new RegexBasedCleanupStrategy($"(?i:{escapedValue})"));
        }

        var tokenPlacements = FindTokenPlacements(
            request.PdfData,
            request.Replacements);

        byte[] redactedPdfData;
        try
        {
            redactedPdfData = CleanPdf(
                request.PdfData,
                strategy,
                tokenPlacements);
        }
        catch (NullReferenceException exception) when (_pdfRasterizer is not null)
        {
            _logger.LogWarning(
                $"PDF cleanup failed for a tagged document; using the rasterized redaction fallback. {exception.Message}");
            redactedPdfData = RedactRasterizedPdf(
                request.PdfData,
                tokenPlacements);
        }

        var response = new RedactPdfResponse
        {
            PdfData = redactedPdfData
        };

        _logger.LogInfo(request,
            "Redacted {PiiItemCount} PII items; output contains {PdfByteCount} bytes",
            request.PiiItems.Count,
            response.PdfData.Length);
        
        _logger.LogMethodEnd();
        return response;
    }

    private static byte[] CleanPdf(
        byte[] pdfData,
        CompositeCleanupStrategy strategy,
        IReadOnlyList<TokenPlacement> tokenPlacements)
    {
        using var inputStream = new MemoryStream(pdfData, writable: false);
        using var outputStream = new MemoryStream();

        using (var pdfDocument = new PdfDocument(
                   new PdfReader(inputStream),
                   new PdfWriter(outputStream)))
        {
            PdfCleaner.AutoSweepCleanUp(pdfDocument, strategy);
            DrawTokens(pdfDocument, tokenPlacements);
        }

        return outputStream.ToArray();
    }

    private byte[] RedactRasterizedPdf(
        byte[] pdfData,
        IReadOnlyList<TokenPlacement> tokenPlacements)
    {
        var rasterized = _pdfRasterizer!.RasterizePdf(
            new RasterizePdfRequest { PdfData = pdfData });
        using var inputStream = new MemoryStream(
            rasterized.PdfData,
            writable: false);
        using var outputStream = new MemoryStream();

        using (var pdfDocument = new PdfDocument(
                   new PdfReader(inputStream),
                   new PdfWriter(outputStream)))
        {
            DrawTokens(pdfDocument, tokenPlacements);
        }

        return outputStream.ToArray();
    }

    private static IReadOnlyList<TokenPlacement> FindTokenPlacements(
        byte[] pdfData,
        IReadOnlyCollection<PdfTextReplacement> replacements)
    {
        var placements = new List<TokenPlacement>();
        using var inputStream = new MemoryStream(pdfData, writable: false);
        using var pdfDocument = new PdfDocument(new PdfReader(inputStream));

        foreach (var replacement in replacements)
        {
            if (string.IsNullOrWhiteSpace(replacement.OriginalValue) ||
                string.IsNullOrWhiteSpace(replacement.ReplacementValue))
            {
                continue;
            }

            var pattern = new Regex(
                Regex.Escape(replacement.OriginalValue),
                RegexOptions.IgnoreCase);

            for (var pageNumber = 1;
                 pageNumber <= pdfDocument.GetNumberOfPages();
                 pageNumber++)
            {
                var page = pdfDocument.GetPage(pageNumber);
                var locationStrategy =
                    new RegexBasedLocationExtractionStrategy(pattern);
                var processor = new PdfCanvasProcessor(locationStrategy);

                processor.ProcessPageContent(page);

                foreach (var location in locationStrategy.GetResultantLocations())
                {
                    var rectangle = location.GetRectangle();

                    placements.Add(new TokenPlacement
                    {
                        PageNumber = pageNumber,
                        Rectangle = new Rectangle(
                            rectangle.GetX(),
                            rectangle.GetY(),
                            rectangle.GetWidth(),
                            rectangle.GetHeight()),
                        Token = replacement.ReplacementValue
                    });
                }
            }
        }

        return placements;
    }

    private static void DrawTokens(
        PdfDocument pdfDocument,
        IReadOnlyList<TokenPlacement> placements)
    {
        if (placements.Count == 0)
        {
            return;
        }

        var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        foreach (var placement in placements)
        {
            var page = pdfDocument.GetPage(placement.PageNumber);
            var canvas = new PdfCanvas(page.NewContentStreamAfter(), page.GetResources(), pdfDocument);
            var fontSize = GetFontSize(font, placement.Token, placement.Rectangle);
            var x = placement.Rectangle.GetX();
            var y = placement.Rectangle.GetY() +
                    (placement.Rectangle.GetHeight() - fontSize) / 2;

            canvas.SaveState();
            canvas.SetFillColor(ColorConstants.WHITE);
            canvas.Rectangle(placement.Rectangle);
            canvas.Fill();
            canvas.RestoreState();

            canvas.BeginText();
            canvas.SetFillColor(ColorConstants.BLACK);
            canvas.SetFontAndSize(font, fontSize);
            canvas.MoveText(x, y);
            canvas.ShowText(placement.Token);
            canvas.EndText();
        }
    }

    private static float GetFontSize(
        PdfFont font,
        string token,
        Rectangle rectangle)
    {
        const float maximumFontSize = 10f;
        const float horizontalPadding = 1f;
        const float verticalScale = 0.8f;

        var availableWidth = Math.Max(
            1f,
            rectangle.GetWidth() - horizontalPadding);
        var widthAtOnePoint = font.GetWidth(token, 1f);
        var sizeForWidth = availableWidth / widthAtOnePoint;
        var sizeForHeight = rectangle.GetHeight() * verticalScale;

        return Math.Min(
            maximumFontSize,
            Math.Min(sizeForWidth, sizeForHeight));
    }

    private sealed class TokenPlacement
    {
        public required int PageNumber { get; init; }

        public required Rectangle Rectangle { get; init; }

        public required string Token { get; init; }
    }
}
