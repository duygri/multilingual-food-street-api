using Microsoft.Extensions.Logging;

namespace NarrationApp.Server.Tests.Support;

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        var scopeValues = ToDictionary(state);
        Scopes.Add(scopeValues);
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(
            logLevel,
            formatter(state, exception),
            exception,
            ToDictionary(state)));
    }

    private static IReadOnlyDictionary<string, object?> ToDictionary<TState>(TState state)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            return pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        return new Dictionary<string, object?>
        {
            ["State"] = state
        };
    }

    internal sealed record LogEntry(
        LogLevel LogLevel,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> State);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
