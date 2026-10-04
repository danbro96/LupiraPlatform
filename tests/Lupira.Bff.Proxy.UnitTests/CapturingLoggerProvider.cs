using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lupira.Bff.Proxy.UnitTests;

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<(LogLevel Level, string Message)> _entries = new();

    public IReadOnlyList<(LogLevel Level, string Message)> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new Logger(_entries);

    public void Dispose()
    {
    }

    private sealed class Logger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
