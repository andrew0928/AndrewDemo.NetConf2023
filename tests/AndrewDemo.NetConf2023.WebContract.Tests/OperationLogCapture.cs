using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

internal sealed class OperationLogCapture : ILoggerProvider
{
    public readonly ConcurrentQueue<Entry> Entries = new();
    public sealed record Entry(LogLevel Level, string Message, string? Exception, Dictionary<string, object?> Fields);
    public ILogger CreateLogger(string categoryName) => new Logger(this);
    public void Dispose() { }
    private sealed class Logger(OperationLogCapture owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => owner.Entries.Enqueue(new(level, formatter(state, exception), exception?.ToString(),
                state is IEnumerable<KeyValuePair<string, object?>> fields ? fields.ToDictionary(x => x.Key, x => x.Value) : new()));
    }
}
