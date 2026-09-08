using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace CognitiveLedger.Parser.PDF;

internal sealed class TimedLogOperation : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private TimedLogOperation(ILogger logger, string operationName)
    {
        _logger = logger;
        _operationName = operationName;
        _logger.LogInformation("Starting {OperationName}", _operationName);
    }

    public static TimedLogOperation Start(ILogger logger, string operationName) =>
        new(logger, operationName);

    public void Dispose()
    {
        _stopwatch.Stop();
        _logger.LogInformation(
            "Finished {OperationName} in {ElapsedMilliseconds} ms",
            _operationName,
            _stopwatch.ElapsedMilliseconds);
    }
}
