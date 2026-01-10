namespace Nouz.Application.Logger;

public interface ILoggerAdapter
{
    /// <summary>
    /// Log a debug message.
    /// </summary>
    void LogDebug(string? message, params object?[] args);

    /// <summary>
    /// Log an information message.
    /// </summary>
    void LogInformation(string? message, params object?[] args);

    /// <summary>
    /// Log a warning message.
    /// </summary>
    void LogWarning(string? message, params object?[] args);

    /// <summary>
    /// Log a warning message with exception.
    /// </summary>
    void LogWarning(Exception exception, string? message, params object?[] args);

    /// <summary>
    /// Log an error message.
    /// </summary>
    void LogError(string? message, params object?[] args);

    /// <summary>
    /// Log an error message with exception.
    /// </summary>
    void LogError(Exception exception, string? message, params object?[] args);
}

public interface ILoggerAdapter<T> : ILoggerAdapter { }