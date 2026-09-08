using CognitiveLedger.Parser.PDF.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Parser.PDF.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitiveLedgerPdfParser(this IServiceCollection services)
    {
        services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
        services.AddSingleton<IPdfRedactor, TextPdfRedactor>();
        services.AddSingleton<IPdfRasterizer, PdfRasterizer>();
        services.AddSingleton<IPiiSanitizer, PdfPiiSanitizer>();
        return services;
    }
}
