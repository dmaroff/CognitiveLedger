using CognitiveLedger.Common;
using CognitiveLedger.Common.Request;
using Microsoft.Extensions.Logging;

namespace CognitiveLedger.Testing;

public sealed class NoOpAppLog<T> : IAppLog<T>
{
    public void LogMethodStart(string methodName = "")
    {
    }

    public void LogMethodStart(RequestBase request, string methodName = "")
    {
    }

    public void LogMethodEnd(string methodName = "")
    {
    }

    public void LogMethodEnd(RequestBase request, string methodName = "")
    {
    }

    public void LogInfo(string text, string methodName = "")
    {
    }

    public void LogInfo(RequestBase request, string text, string methodName = "")
    {
    }

    public void LogInfo<TValue1, TValue2>(
        RequestBase request,
        string message,
        TValue1 value1,
        TValue2 value2,
        string methodName = "")
    {
    }

    public void LogWarning(string text, string methodName = "")
    {
    }

    public void LogWarning(RequestBase request, string text, string methodName = "")
    {
    }

    public void LogError(string text, string methodName = "")
    {
    }

    public void LogError(RequestBase request, string text, string methodName = "")
    {
    }

    public void LogError(Exception ex, string methodName = "")
    {
    }

    public void LogError(RequestBase request, Exception ex, string methodName = "")
    {
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}