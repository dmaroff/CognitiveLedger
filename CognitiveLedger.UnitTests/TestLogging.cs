using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CognitiveLedger.UnitTests;

[SetUpFixture]
public sealed class TestLoggingSetup
{
    [OneTimeSetUp]
    public void Initialize() => TestLogging.Initialize();

    [OneTimeTearDown]
    public void Dispose() => TestLogging.Dispose();
}

internal static class TestLogging
{
    private static ILoggerFactory? _loggerFactory;
    private static Serilog.Core.Logger? _fileLogger;

    public static void Initialize()
    {
        var testDirectory = TestContext.CurrentContext.TestDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(testDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var minimumLevelName = configuration["Logging:MinimumLevel"] ?? "Information";
        var microsoftMinimumLevel = Enum.Parse<LogLevel>(minimumLevelName, ignoreCase: true);
        var serilogMinimumLevel = Enum.Parse<Serilog.Events.LogEventLevel>(
            minimumLevelName,
            ignoreCase: true);

        var relativeLogPath = configuration["Logging:File:Path"]
                              ?? "logs/cognitive-ledger-.log";
        var logPath = Path.Combine(testDirectory, relativeLogPath);
        var logDirectory = Path.GetDirectoryName(logPath)
                           ?? throw new InvalidOperationException(
                               "The configured log path has no directory.");
        Directory.CreateDirectory(logDirectory);

        _fileLogger = new LoggerConfiguration()
            .MinimumLevel.Is(serilogMinimumLevel)
            .WriteTo.File(
                logPath,
                rollingInterval: Enum.Parse<RollingInterval>(
                    configuration["Logging:File:RollingInterval"] ?? "Day",
                    ignoreCase: true),
                fileSizeLimitBytes: long.Parse(
                    configuration["Logging:File:FileSizeLimitBytes"] ?? "5242880"),
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: int.Parse(
                    configuration["Logging:File:RetainedFileCountLimit"] ?? "10"),
                shared: bool.Parse(configuration["Logging:File:Shared"] ?? "true"))
            .CreateLogger();

        _loggerFactory = LoggerFactory.Create(builder =>
            builder
                .SetMinimumLevel(microsoftMinimumLevel)
                .AddSerilog(_fileLogger, dispose: false)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = bool.Parse(
                        configuration["Logging:Console:SingleLine"] ?? "true");
                    options.TimestampFormat =
                        configuration["Logging:Console:TimestampFormat"] ?? "HH:mm:ss.fff ";
                }));

        TestContext.Progress.WriteLine($"Log file directory: {logDirectory}");
    }

    public static ILogger<T> CreateLogger<T>() =>
        (_loggerFactory ?? throw new InvalidOperationException("Test logging is not initialized."))
        .CreateLogger<T>();

    public static void Dispose()
    {
        _loggerFactory?.Dispose();
        _fileLogger?.Dispose();
        _loggerFactory = null;
        _fileLogger = null;
    }
}
