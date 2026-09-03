using Avalonia;
using System;
using System.Threading.Tasks;
using CognitiveLedger.AI.AIPlatform;
using CognitiveLedger.AI.Analyzers;
using CognitiveLedger.AI.Identifiers;
using CognitiveLedger.AI.Services.Ollama;
using CognitiveLedger.AI.Services.Ollama.DependencyInjection;
using CognitiveLedger.Desktop.Helpers;
using CognitiveLedger.Desktop.ViewModels;
using CognitiveLedger.Parser.PDF;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CognitiveLedger.Desktop;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);
        
        using var host = builder.ConfigureServices(services =>
            {
                services.AddCognitiveLedgerOllama(options =>
                {
                    options.ModelName = "qwen3:8b";
                });
                services.AddSingleton<MainViewModel>();
                services.AddTransient<OllamaDownloadViewModel>();
                //services.AddTransient<IPdfTextExtractor, PdfTextExtractor>();
                services.AddSingleton<IAiPlatform>(sp =>
                {
                    var options = sp.GetRequiredService<IOptions<OllamaServiceOptions>>().Value;
                    return new OllamaPlatform(options.Host, options.Port);
                });
                services.AddTransient<IStatementAnalyzerFactory, StatementAnalyzerFactory>();
                services.AddTransient<IBankIdentifier, BankIdentifier>();
                
                // Bank Analyzers
                services.AddKeyedTransient<IStatementAnalyzer, AmazonSynchronyBankAnalyzer>("SynchronyBank");
            })
            .Build();

        App.Host = host;
        host.Start();

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // Run on a thread-pool thread rather than blocking here directly: this is the
            // Avalonia main thread, and StopAsync's internal awaits (ours and the host's own)
            // capture the current SynchronizationContext by default. Blocking synchronously
            // on that same thread while a continuation tries to resume on it deadlocks.
            // ReSharper disable once AccessToDisposedClosure - GetResult() blocks until this
            // completes, so it always runs before the `using` disposes host.
            Task.Run(() => host.StopAsync()).GetAwaiter().GetResult();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}