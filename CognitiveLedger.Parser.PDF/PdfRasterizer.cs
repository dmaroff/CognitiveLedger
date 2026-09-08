using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Parser.PDF.Request;
using CognitiveLedger.Parser.PDF.Response;
using iText.IO.Image;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using PDFtoImage;
using SkiaSharp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CognitiveLedger.Parser.PDF;

/// <summary>
/// Renders every source page and builds a new image-only PDF. No source PDF
/// objects, metadata, annotations, form fields, attachments, or text layers
/// are copied into the result.
/// </summary>
public sealed class PdfRasterizer : IPdfRasterizer
{
    private readonly ILogger<PdfRasterizer> _logger;

    public PdfRasterizer(ILogger<PdfRasterizer>? logger = null)
    {
        _logger = logger ?? NullLogger<PdfRasterizer>.Instance;
    }

    public RasterizePdfResponse RasterizePdf(RasterizePdfRequest request)
    {
        using var operation = TimedLogOperation.Start(_logger, nameof(RasterizePdf));
        ArgumentNullException.ThrowIfNull(request);

        if (request.PdfData is null || request.PdfData.Length == 0)
        {
            throw new ArgumentException("PDF data cannot be null or empty.", nameof(request));
        }

        if (request.Dpi is < 72 or > 600)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Dpi,
                "Rasterization DPI must be between 72 and 600.");
        }

        using var inputStream = new MemoryStream(request.PdfData, writable: false);
        using var outputStream = new MemoryStream();
        using var writer = new PdfWriter(outputStream);
        using var outputDocument = new PdfDocument(writer);

        var pageCount = 0;
        var options = new RenderOptions(Dpi: request.Dpi);

        foreach (var bitmap in Conversion.ToImages(inputStream, options: options))
        {
            using (bitmap)
            using (var encodedImage = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                if (encodedImage is null)
                {
                    throw new InvalidOperationException(
                        $"Could not encode rendered PDF page {pageCount + 1}.");
                }

                var pageWidth = bitmap.Width * 72f / request.Dpi;
                var pageHeight = bitmap.Height * 72f / request.Dpi;
                var pageSize = new PageSize(pageWidth, pageHeight);
                var page = outputDocument.AddNewPage(pageSize);
                var imageData = ImageDataFactory.Create(encodedImage.ToArray());
                var pageBounds = new Rectangle(0, 0, pageWidth, pageHeight);

                new PdfCanvas(page)
                    .AddImageFittedIntoRectangle(imageData, pageBounds, false);

                pageCount++;
            }
        }

        if (pageCount == 0)
        {
            throw new InvalidOperationException("The source PDF contains no renderable pages.");
        }

        outputDocument.Close();

        var response = new RasterizePdfResponse
        {
            PdfData = outputStream.ToArray(),
            PageCount = pageCount,
            Dpi = request.Dpi
        };

        _logger.LogInformation(
            "Rasterized {PageCount} pages at {Dpi} DPI into {PdfByteCount} bytes",
            response.PageCount,
            response.Dpi,
            response.PdfData.Length);
        return response;
    }
}
