using Microsoft.Extensions.DependencyInjection;

namespace CognitiveLedger.Parser.PDF.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCognitiveLedgerPdfParser(this IServiceCollection services)
    {
        //services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
        //services.AddSingleton<IPdfRedactor, PdfRedactor>();
        return services;
    }
}
