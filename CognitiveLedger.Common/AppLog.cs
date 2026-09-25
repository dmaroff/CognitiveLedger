using System.Runtime.CompilerServices;
using CognitiveLedger.Common.Request;
using Microsoft.Extensions.Logging;

namespace CognitiveLedger.Common;

public sealed class AppLog<T>(ILogger<T> logger) : ILogger<T>
{
    private readonly ILogger<T> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public void LogMethodStart([CallerMemberName] string methodName = "")
    {
        _logger.LogInformation(
            "{ClassName}::{MethodName} ==> Start",
            typeof(T).Name,
            methodName);
    }
    
    public void LogMethodStart(
        RequestBase request,
        [CallerMemberName] string methodName = "")
    {
        _logger.LogInformation(
            "{ClassName}::{MethodName} ==> [{Request}] => Start",
            typeof(T).Name,
            methodName,
            request);
    }

    public void LogMethodEnd([CallerMemberName] string methodName = "")
    {
        _logger.LogInformation(
            "{ClassName}::{MethodName} ==> End",
            typeof(T).Name,
            methodName);
    }
    
    public void LogMethodEnd(
        RequestBase request,
        [CallerMemberName] string methodName = "")
    {
        _logger.LogInformation(
            "{ClassName}::{MethodName} ==> [{Request}] => End",
            typeof(T).Name,
            methodName,
            request);
    }
    
    public void LogInfo(
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogInformation(
            "{ClassName}::{MethodName} ==> {Text}",
            typeof(T).Name,
            methodName,
            text);
    }
    
    public void LogInfo(
        RequestBase request,
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogInformation(
            "{ClassName}::{MethodName} ==> [{Request}] => {Text}",
            typeof(T).Name,
            methodName,
            request,
            text);
    }
    
    public void LogWarning(
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogWarning(
            "{ClassName}::{MethodName} ==> {Text}",
            typeof(T).Name,
            methodName,
            text);
    }
    
    public void LogWarning(
        RequestBase request,
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogWarning(
            "{ClassName}::{MethodName} ==> [{Request}] => {Text}",
            typeof(T).Name,
            methodName,
            request,
            text);
    }
    
    public void LogError(
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogError(
            "{ClassName}::{MethodName} ==> {Text}",
            typeof(T).Name,
            methodName,
            text);
    }
    
    public void LogError(
        RequestBase request,
        string text,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogError(
            "{ClassName}::{MethodName} ==> [{Request}] => {Text}",
            typeof(T).Name,
            methodName,
            request,
            text);
    }
    
    public void LogError(
        Exception ex,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogError(
            ex,
            "{ClassName}::{MethodName}",
            typeof(T).Name,
            methodName);
    }
    
    public void LogError(
        RequestBase request,
        Exception ex,
        [CallerMemberName] string methodName = "")
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogError(
            ex,
            "{ClassName}::{MethodName} ==> [{Request}] => {Text}",
            typeof(T).Name,
            methodName,
            request,
            "");
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) =>
        _logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _logger.Log(logLevel, eventId, state, exception, formatter);
}
