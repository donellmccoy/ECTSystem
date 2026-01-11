namespace AF.ECT.Tests.Common;

/// <summary>
/// Base class for all unit tests requiring proper async initialization and cleanup.
/// Implements <see cref="IAsyncLifetime"/> for xUnit's async lifecycle management.
/// Use this for tests that need async setup/teardown without blocking.
/// </summary>
public abstract class UnitTestBase : IAsyncLifetime
{
    /// <summary>
    /// Async initialization hook called before each test runs.
    /// Override to perform async setup (e.g., creating mocks, initializing databases).
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Async cleanup hook called after each test completes.
    /// Override to perform async cleanup (e.g., disposing resources, clearing data).
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;
}
