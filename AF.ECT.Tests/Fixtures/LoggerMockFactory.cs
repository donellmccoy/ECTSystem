namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Factory for creating configured Mock&lt;ILogger&gt; and Mock&lt;ILogger&lt;T&gt;&gt; instances.
/// Reduces repetitive mock creation across test classes and ensures consistent logger behavior.
/// Provides safe no-op logger implementations for testing without side effects.
/// </summary>
public class LoggerMockFactory
{
    /// <summary>
    /// Creates a new Mock&lt;ILogger&gt; with default no-op behavior.
    /// Prevents null reference exceptions and silently ignores log calls.
    /// </summary>
    /// <returns>A configured mock ILogger instance</returns>
    public static Mock<ILogger> CreateDefaultMock()
    {
        var mockLogger = new Mock<ILogger>();
        
        // Configure default behavior for logging methods - Log is void so no Returns
        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        
        return mockLogger;
    }

    /// <summary>
    /// Creates a new Mock&lt;ILogger&lt;T&gt;&gt; with default no-op behavior.
    /// Prevents null reference exceptions and silently ignores log calls.
    /// </summary>
    /// <typeparam name="T">The logger type parameter</typeparam>
    /// <returns>A configured mock ILogger&lt;T&gt; instance</returns>
    public static Mock<ILogger<T>> CreateDefaultMock<T>() where T : class
    {
        var mockLogger = new Mock<ILogger<T>>();
        
        // Configure default behavior for logging methods - Log is void so no Returns
        mockLogger
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        
        return mockLogger;
    }

    /// <summary>
    /// Creates a mock ILogger&lt;T&gt; that tracks all log calls for assertion.
    /// Useful for testing logging behavior without capturing actual log messages.
    /// </summary>
    /// <typeparam name="T">The logger type parameter</typeparam>
    /// <returns>A configured mock ILogger&lt;T&gt; instance</returns>
    public static Mock<ILogger<T>> CreateTrackingMock<T>() where T : class
    {
        var mockLogger = CreateDefaultMock<T>();
        
        // Verify calls can be made with Verify() without setup
        mockLogger
            .Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
            .Returns(true);
        
        return mockLogger;
    }

    /// <summary>
    /// Creates a mock logger with custom behavior.
    /// </summary>
    /// <typeparam name="T">The logger type parameter</typeparam>
    /// <param name="configureAction">Action to configure custom logger behavior</param>
    /// <returns>A configured mock ILogger&lt;T&gt; instance</returns>
    public static Mock<ILogger<T>> CreateCustomMock<T>(Action<Mock<ILogger<T>>> configureAction) where T : class
    {
        var mockLogger = CreateDefaultMock<T>();
        configureAction(mockLogger);
        return mockLogger;
    }
}
