using System.Text.Json.Serialization;
using CognitiveLedger.AI.OpenAI;
using CognitiveLedger.Common;
using CognitiveLedger.Data.DependencyInjection;
using CognitiveLedger.Parser.PDF.DependencyInjection;
using CognitiveLedger.Parser.PDF.Interfaces;
using CognitiveLedger.Services.Importer.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var fileLogger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(fileLogger, dispose: true);

builder.Services.AddOpenApi();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpClient();
builder.Services.AddTransient(typeof(AppLog<>));
builder.Services.AddScoped<IAppConfiguration, AppConfiguration>();
builder.Services.AddScoped<IOpenAiPdfStatementReader, OpenAiPdfStatementReader>();
builder.Services.AddCognitiveLedgerData(builder.Configuration);
builder.Services.AddSingleton<IPiiDetector, ProvidedValuePiiDetector>();
builder.Services.AddCognitiveLedgerPdfParser();

builder.Services.AddSingleton<ILocalQueueService, LocalQueueService>();
builder.Services.AddScoped<IImportService, ImportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
