namespace AF.ECT.Tests.Fixtures;

using System.Collections.Concurrent;
using AF.ECT.Server.Services;
using AF.ECT.Data.Interfaces;
using Grpc.Core;

/// <summary>
/// Provides shared, cached mock instances across test collections.
/// Reduces mock creation overhead and enables safe reuse of stateless mocks.
/// Thread-safe implementation using ConcurrentDictionary for concurrent test execution.
/// </summary>
public class SharedMockFixture : IAsyncLifetime
{
    private static readonly ConcurrentDictionary<string, object> _mockCache = new();
    private readonly object _lockObj = new();

    /// <summary>
    /// Gets or creates a cached Mock&lt;ILogger&lt;T&gt;&gt; instance.
    /// Safe to reuse across multiple tests since loggers are stateless.
    /// </summary>
    /// <typeparam name="T">The logger type parameter</typeparam>
    /// <returns>A cached mock ILogger&lt;T&gt; instance</returns>
    public Mock<ILogger<T>> GetOrCreateLoggerMock<T>() where T : class
    {
        var key = $"logger_{typeof(T).FullName}";
        var mock = _mockCache.GetOrAdd(key, _ => LoggerMockFactory.CreateDefaultMock<T>());
        return (Mock<ILogger<T>>)mock;
    }

    /// <summary>
    /// Gets or creates a cached Mock&lt;IDataService&gt; instance.
    /// Safe to reuse across multiple tests since mocks use default empty return values.
    /// </summary>
    /// <returns>A cached mock IDataService instance</returns>
    public Mock<IDataService> GetOrCreateDataServiceMock()
    {
        const string key = "dataservice_default";
        var mock = _mockCache.GetOrAdd(key, _ => DataServiceMockFactory.CreateDefaultMock());
        return (Mock<IDataService>)mock;
    }

    /// <summary>
    /// Gets or creates a cached Mock&lt;ServerCallContext&gt; instance.
    /// Safe to reuse across multiple tests for basic gRPC context simulation.
    /// </summary>
    /// <returns>A cached mock ServerCallContext instance</returns>
    public ServerCallContext GetOrCreateServerCallContext()
    {
        const string key = "servercallcontext_default";
        var mock = _mockCache.GetOrAdd(key, _ =>
        {
            var mockContext = new Mock<ServerCallContext>(MockBehavior.Loose);
            return mockContext.Object;
        });
        return (ServerCallContext)mock;
    }

    /// <summary>
    /// Gets or creates a custom cached mock with a specific key.
    /// Enables fine-grained control over mock reuse within a test collection.
    /// </summary>
    /// <typeparam name="T">The type being mocked</typeparam>
    /// <param name="key">Unique cache key for this mock variant</param>
    /// <param name="factory">Factory function to create the mock if not cached</param>
    /// <returns>A cached mock instance</returns>
    public Mock<T> GetOrCreateCustomMock<T>(string key, Func<Mock<T>> factory) where T : class
    {
        var cacheKey = $"custom_{typeof(T).FullName}_{key}";
        var mock = _mockCache.GetOrAdd(cacheKey, _ => factory());
        return (Mock<T>)mock;
    }

    /// <summary>
    /// Clears the mock cache. Called during fixture disposal.
    /// Warning: Only clear if you're certain no active tests are using the mocks.
    /// </summary>
    public void ClearCache()
    {
        lock (_lockObj)
        {
            _mockCache.Clear();
        }
    }

    /// <summary>
    /// Initializes the fixture - prepares common mocks for reuse.
    /// </summary>
    public Task InitializeAsync()
    {
        // Pre-populate cache with commonly used mocks
        _ = GetOrCreateLoggerMock<WorkflowServiceImpl>();
        _ = GetOrCreateDataServiceMock();
        _ = GetOrCreateServerCallContext();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the fixture and clears the mock cache.
    /// </summary>
    public Task DisposeAsync()
    {
        ClearCache();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Collection definition for tests using SharedMockFixture.
/// </summary>
[CollectionDefinition("Shared Mocks")]
public class SharedMockCollection : ICollectionFixture<SharedMockFixture>
{
    // This class has no code, and is never instantiated. Its purpose is purely
    // to define the collection that tests can join in order to use the shared mock fixture.
}
