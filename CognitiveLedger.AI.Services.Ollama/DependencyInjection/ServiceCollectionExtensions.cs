using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CognitiveLedger.AI.Services.Ollama.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a private, per-application Ollama server as a hosted service: it is
    /// downloaded (if needed), started, health-checked, and stopped alongside the host.
    /// </summary>
    public static IServiceCollection AddCognitiveLedgerOllama(
        this IServiceCollection services,
        Action<OllamaServiceOptions> configureOptions)
    {
        services.Configure(configureOptions);

        // Registered as a concrete singleton (not just AddHostedService<OllamaService>()) so
        // consumers can resolve OllamaService directly - e.g. to observe its Ready task -
        // while the host still starts/stops the same instance via IHostedService.
        services.AddSingleton<OllamaService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<OllamaService>());

        return services;
    }
}
