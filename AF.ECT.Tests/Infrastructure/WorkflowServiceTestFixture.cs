using AF.ECT.Server.Services;
using AF.ECT.Server.Services.Interfaces;
using AF.ECT.Data.Interfaces;
using Polly.CircuitBreaker;
using Microsoft.EntityFrameworkCore;

namespace AF.ECT.Tests.Infrastructure;

/// <summary>
/// Provides reusable test fixtures for WorkflowService testing.
/// Centralizes mock setup and service instantiation to reduce test boilerplate.
/// </summary>
public class WorkflowServiceTestFixture : IDisposable
{
    /// <summary>
    /// Gets the mock logger instance.
    /// </summary>
    public Mock<ILogger<WorkflowServiceImpl>> MockLogger { get; }

    /// <summary>
    /// Gets the mock data service instance.
    /// </summary>
    public Mock<IDataService> MockDataService { get; }

    /// <summary>
    /// Gets the mock resilience service instance.
    /// </summary>
    public Mock<IResilienceService> MockResilienceService { get; }

    /// <summary>
    /// Gets the test resilience service (non-mocked) for unit tests.
    /// </summary>
    public IResilienceService TestResilienceService { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowServiceTestFixture"/> class.
    /// </summary>
    public WorkflowServiceTestFixture()
    {
        MockLogger = new Mock<ILogger<WorkflowServiceImpl>>();
        MockDataService = new Mock<IDataService>();
        MockResilienceService = new Mock<IResilienceService>();
        TestResilienceService = new DefaultTestResilienceService();
    }

    /// <summary>
    /// Creates a new WorkflowServiceImpl instance with configured mocks.
    /// </summary>
    /// <returns>A new WorkflowServiceImpl instance for testing.</returns>
    public WorkflowServiceImpl CreateService() => 
        new(MockLogger.Object, MockDataService.Object, TestResilienceService);

    /// <summary>
    /// Resets all mock setups for a fresh test state.
    /// </summary>
    public void ResetMocks()
    {
        MockLogger.Reset();
        MockDataService.Reset();
        MockResilienceService.Reset();
    }

    /// <summary>
    /// Verifies that the data service was called as expected.
    /// </summary>
    public void VerifyDataServiceCalls() => MockDataService.Verify();

    /// <summary>
    /// Configures the mock data service to throw a specific exception on the next call.
    /// </summary>
    /// <param name="exception">The exception to throw.</param>
    public void ConfigureDataServiceToThrow(Exception exception) =>
        MockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    /// <summary>
    /// Disposes the fixture resources.
    /// </summary>
    public void Dispose()
    {
        MockLogger?.Reset();
        MockDataService?.Reset();
        MockResilienceService?.Reset();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Provides base functionality for integration test fixtures.
/// </summary>
public abstract class IntegrationTestFixture
{
    /// <summary>
    /// Gets the database context for testing.
    /// </summary>
    protected DbContext DbContext { get; set; } = null!;

    /// <summary>
    /// Initializes the fixture.
    /// </summary>
    public virtual void Initialize()
    {
        // Derived classes override this to set up database context
    }

    /// <summary>
    /// Disposes the fixture resources.
    /// </summary>
    public virtual void Cleanup()
    {
        DbContext?.Dispose();
    }
}

/// <summary>
/// Test implementation of IResilienceService that executes operations without retry logic.
/// Used for unit testing to bypass resilience patterns.
/// </summary>
public class DefaultTestResilienceService : IResilienceService
{
    /// <summary>
    /// Executes the HTTP request without any resilience policies.
    /// </summary>
    public async Task<HttpResponseMessage> ExecuteResilientHttpRequestAsync(Func<Task<HttpResponseMessage>> action)
    {
        return await action();
    }

    /// <summary>
    /// Executes the operation without any retry logic.
    /// </summary>
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }

    /// <summary>
    /// Executes the database operation without retry logic.
    /// </summary>
    public async Task<T> ExecuteDatabaseOperationAsync<T>(Func<Task<T>> action)
    {
        return await action();
    }

    /// <summary>
    /// Gets the circuit breaker state (always Closed in test).
    /// </summary>
    public CircuitState CircuitBreakerState => CircuitState.Closed;

    /// <summary>
    /// Gets the last exception (null in test).
    /// </summary>
    public Exception? LastException => null;

    /// <summary>
    /// Resets the circuit breaker (no-op in test).
    /// </summary>
    public void ResetCircuitBreaker()
    {
        // No-op in test
    }
}

/// <summary>
/// Fixture for gRPC client testing with mock server setup.
/// </summary>
public class GrpcClientTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Initializes the fixture asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Implementation would set up test gRPC server
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the fixture resources asynchronously.
    /// </summary>
    public async Task DisposeAsync()
    {
        await Task.CompletedTask;
    }
}

/// <summary>
/// Fixture for data layer testing with real database context.
/// </summary>
public class DatabaseTestFixture : IAsyncLifetime
{
    private readonly string _connectionString;
    private DbContext? _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTestFixture"/> class.
    /// </summary>
    public DatabaseTestFixture(string? connectionString = null)
    {
        _connectionString = connectionString ?? "Data Source=.;Initial Catalog=test_ect;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;";
    }

    /// <summary>
    /// Gets the database context.
    /// </summary>
    public DbContext Context => _dbContext ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// Initializes the fixture asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create and configure context
        var options = new DbContextOptionsBuilder<DbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _dbContext = new DbContext(options);
        
        // Ensure database is created and migrations are applied
        await _dbContext.Database.MigrateAsync();
        await Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the fixture resources asynchronously.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            // Clean up test data if needed
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Clears all data from specified tables for a fresh test state.
    /// </summary>
    public async Task ClearTablesAsync(params string[] tableNames)
    {
        foreach (var tableName in tableNames)
        {
            // Validate table name to prevent SQL injection
            if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                throw new ArgumentException($"Invalid table name: {tableName}", nameof(tableNames));
            }

            await _dbContext!.Database.ExecuteSqlAsync($"TRUNCATE TABLE [dbo].[{tableName}]");
        }
    }
}
