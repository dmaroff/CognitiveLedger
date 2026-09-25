using CognitiveLedger.Common;
using CognitiveLedger.Data.DependencyInjection;
using CognitiveLedger.Services.LedgerMcp.Security;
using CognitiveLedger.Services.LedgerMcp.Tools.GetAccountOverview;
using CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;
using CognitiveLedger.Services.LedgerMcp.Tools.SummarizeTransactions;
using ModelContextProtocol.AspNetCore;
using Serilog;

namespace CognitiveLedger.Services.LedgerMcp;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile(
                "appsettings.local.json",
                optional: true,
                reloadOnChange: true);
        }

        builder.Services.AddTransient(typeof(AppLog<>));
        
        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCognitiveLedgerData(builder.Configuration);
        builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        builder.Services.AddScoped<IAccountOverviewQuery, AccountOverviewQuery>();
        builder.Services.AddScoped<ITransactionSearchQuery, TransactionSearchQuery>();
        builder.Services.AddScoped<ITransactionSummaryQuery, TransactionSummaryQuery>();

        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => { options.SessionMode = HttpServerSessionMode.Stateless; })
            .WithToolsFromAssembly();

        var app = builder.Build();

        app.MapMcp("/mcp");

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var urls = app.Urls.Count > 0
                ? string.Join(", ", app.Urls)
                : "unknown";

            app.Logger.LogInformation(
                "CognitiveLedger.Services.LedgerMcp is listening at {Urls}",
                urls);
        });
        
        app.Run();
    }
}
