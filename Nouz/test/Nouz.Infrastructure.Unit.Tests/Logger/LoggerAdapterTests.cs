using Microsoft.Extensions.Logging;
using Nouz.Infrastructure.Logger;
using NSubstitute;

namespace Nouz.Infrastructure.Unit.Tests.Logger;

public class LoggerAdapterTests
{
    private readonly LoggerAdapter<LoggerAdapterTests> _loggerAdapter;
    private readonly ILogger<LoggerAdapterTests> _logger = Substitute.For<ILogger<LoggerAdapterTests>>();

    public LoggerAdapterTests()
    {
        _loggerAdapter = new LoggerAdapter<LoggerAdapterTests>(_logger);
    }

    [Fact]
    public void LogDebug_ShouldCallLogDebugOnLogger()
    {
        // Arrange
        const string message = "Debug message";
        var args = new object[] { "arg1", "arg2" };

        // Act
        _loggerAdapter.LogDebug(message, args);

        // Assert
        _logger.Received(1).LogDebug(message, args);
    }

    [Fact]
    public void LogInformation_ShouldCallLogInformationOnLogger()
    {
        // Arrange
        const string message = "Information message";
        var args = new object[] { "arg1", "arg2" };

        // Act
        _loggerAdapter.LogInformation(message, args);

        // Assert
        _logger.Received(1).LogInformation(message, args);
    }

    [Fact]
    public void LogWarning_ShouldCallLogWarningOnLogger()
    {
        // Arrange
        const string message = "Warning message";
        var args = new object[] { "arg1", "arg2" };

        // Act
        _loggerAdapter.LogWarning(message, args);

        // Assert
        _logger.Received(1).LogWarning(message, args);
    }

    [Fact]
    public void LogWarning_ShouldCallLogWarningOnLogger_WhenExceptionIsProvided()
    {
        // Arrange
        const string message = "Warning message";
        var args = new object[] { "arg1", "arg2" };
        var exception = new Exception("Test exception");

        // Act
        _loggerAdapter.LogWarning(exception, message, args);

        // Assert
        _logger.Received(1).LogWarning(exception, message, args);
    }

    [Fact]
    public void LogError_ShouldCallLogErrorOnLogger()
    {
        // Arrange
        const string message = "Error message";
        var args = new object[] { "arg1", "arg2" };

        // Act
        _loggerAdapter.LogError(message, args);

        // Assert
        _logger.Received(1).LogError(message, args);
    }

    [Fact]
    public void LogError_ShouldCallLogErrorOnLogger_WhenExceptionIsProvided()
    {
        // Arrange
        const string message = "Error message";
        var args = new object[] { "arg1", "arg2" };
        var exception = new Exception("Test exception");

        // Act
        _loggerAdapter.LogError(exception, message, args);

        // Assert
        _logger.Received(1).LogError(exception, message, args);
    }
}