using CognitiveLedger.Data.DependencyInjection;
using CognitiveLedger.Services.LedgerMcp.Security;
using CognitiveLedger.Services.LedgerMcp.Tools.SearchTransactions;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.local.json",
        optional: true,
        reloadOnChange: true);
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddCognitiveLedgerData(builder.Configuration);
builder.Services.AddScoped<ICurrentUserContext, CurrentUserContext>();
builder.Services.AddScoped<ITransactionSearchQuery, TransactionSearchQuery>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("/mcp");

app.Run();
