using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Oleander.StrResGen;

internal class TraceLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var msg = formatter(state, exception);

        switch (logLevel)
        {
            case LogLevel.None:
                return;
            case LogLevel.Trace:
                Trace.WriteLine(msg);
                break;
            case LogLevel.Debug:
                Debug.WriteLine(msg);
                break;
            case LogLevel.Information:
                Trace.TraceInformation(msg);
                break;
            case LogLevel.Warning:
                Trace.TraceWarning(msg);
                break;
            case LogLevel.Critical:
            case LogLevel.Error:
                Trace.TraceError(msg);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }
}