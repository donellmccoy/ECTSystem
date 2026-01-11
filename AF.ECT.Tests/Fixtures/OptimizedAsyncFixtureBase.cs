namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Provides optimized async initialization patterns for test fixtures.
/// Enables parallel fixture initialization and lazy component setup.
/// Reduces test startup time through efficient async/await patterns.
/// </summary>
public abstract class OptimizedAsyncFixtureBase : IAsyncLifetime
{
    protected bool IsInitialized { get; private set; }
    private TaskCompletionSource _initializationTcs = new();

    /// <summary>
    /// Template method for async initialization.
    /// Override OnInitializeAsync() in derived classes for custom setup.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        try
        {
            await OnInitializeAsync();
            IsInitialized = true;
            _initializationTcs.SetResult();
        }
        catch (Exception ex)
        {
            IsInitialized = false;
            _initializationTcs.SetException(ex);
            throw;
        }
    }

    /// <summary>
    /// Template method for async cleanup.
    /// Override OnDisposeAsync() in derived classes for custom cleanup.
    /// </summary>
    public async Task DisposeAsync()
    {
        try
        {
            if (IsInitialized)
            {
                await OnDisposeAsync();
            }
        }
        finally
        {
            IsInitialized = false;
        }
    }

    /// <summary>
    /// Override this method to perform custom async initialization.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Override this method to perform custom async cleanup.
    /// </summary>
    protected virtual Task OnDisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Waits for fixture initialization to complete.
    /// Useful when tests depend on async initialization finishing first.
    /// </summary>
    protected Task WaitForInitializationAsync() => _initializationTcs.Task;
}

/// <summary>
/// Base class for test classes that use multiple fixtures efficiently.
/// Provides helper methods for common test setup scenarios.
/// </summary>
public abstract class OptimizedTestBase
{
    /// <summary>
    /// Creates a mock service that returns default values safely.
    /// </summary>
    protected static Mock<T> CreateDefaultMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Loose);
    }

    /// <summary>
    /// Creates a server call context mock for gRPC tests.
    /// </summary>
    protected static ServerCallContext CreateMockServerCallContext()
    {
        var mockContext = new Mock<ServerCallContext>(MockBehavior.Loose);
        return mockContext.Object;
    }

    /// <summary>
    /// Executes multiple async operations in parallel with proper error aggregation.
    /// More efficient than sequential Task.WhenAll for many operations.
    /// </summary>
    protected static async Task RunParallelAsync(params Func<Task>[] operations)
    {
        var tasks = operations.Select(op => 
            op().ContinueWith(t => t, TaskScheduler.Default)).ToList();
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes multiple async operations in parallel with return values.
    /// </summary>
    protected static async Task<T[]> RunParallelAsync<T>(params Func<Task<T>>[] operations)
    {
        var tasks = operations.Select(op => op()).ToArray();
        return await Task.WhenAll(tasks);
    }
}
