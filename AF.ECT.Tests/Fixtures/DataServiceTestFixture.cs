using AF.ECT.Data;
using AF.ECT.Data.Interfaces;
using AF.ECT.Data.Services;

namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Fixture for data layer tests providing in-memory database context and test data management.
/// Handles EF Core context creation and cleanup for consistent test isolation.
/// </summary>
public class DataServiceTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the EF Core database context options builder.
    /// </summary>
    private DbContextOptionsBuilder<ALODContext> ContextOptionsBuilder { get; set; } = null!;

    /// <summary>
    /// Gets the current database context instance.
    /// </summary>
    public ALODContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Gets the data service instance under test.
    /// </summary>
    public IDataService DataServiceUnderTest { get; private set; } = null!;

    /// <summary>
    /// Asynchronously initializes the fixture with a fresh in-memory database.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Create fresh in-memory database for this test
        var databaseName = Guid.NewGuid().ToString();
        ContextOptionsBuilder = new DbContextOptionsBuilder<ALODContext>()
            .UseInMemoryDatabase(databaseName);

        DbContext = new ALODContext(ContextOptionsBuilder.Options);
        
        // Ensure database is created
        await DbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Asynchronously cleans up fixture resources and deletes the test database.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Adds test data to the database and saves changes.
    /// </summary>
    /// <typeparam name="T">The entity type to add</typeparam>
    /// <param name="entity">The entity instance to add</param>
    public async Task AddTestDataAsync<T>(T entity)
        where T : class
    {
        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Adds multiple test entities to the database.
    /// </summary>
    /// <typeparam name="T">The entity type to add</typeparam>
    /// <param name="entities">The entities to add</param>
    public async Task AddTestDataRangeAsync<T>(params T[] entities)
        where T : class
    {
        DbContext.AddRange(entities);
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all data from the database (for test isolation).
    /// </summary>
    public async Task ClearDatabaseAsync()
    {
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Creates a fresh context for mid-test assertions (simulates new connection).
    /// </summary>
    /// <returns>A new ALODContext instance</returns>
    public ALODContext CreateFreshContext()
    {
        return new ALODContext(ContextOptionsBuilder.Options);
    }
}
