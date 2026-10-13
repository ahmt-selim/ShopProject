namespace Backend.Logging;

public static class FileLoggingExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string directory)
    {
        builder.AddProvider(new FileLoggerProvider(directory));
        return builder;
    }
}

public sealed class FileLoggerProvider(string directory) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, directory);
    public void Dispose() { }
}

public sealed class FileLogger : ILogger
{
    private static readonly object Gate = new();
    private readonly string _category;
    private readonly string _dir;

    public FileLogger(string category, string directory)
    {
        _category = category;
        _dir = directory;
        Directory.CreateDirectory(_dir);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {_category}: {formatter(state, exception)}";
        if (exception is not null) line += Environment.NewLine + exception;
        var path = Path.Combine(_dir, $"shop-{DateTime.Now:yyyy-MM-dd}.log");
        lock (Gate) File.AppendAllText(path, line + Environment.NewLine);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
