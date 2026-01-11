namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for managing test data lifecycle: creation, cleanup, and transaction management.
/// Ensures test data isolation and prevents data leakage between test runs.
/// </summary>
public class TestDataCleanupHelper : IAsyncLifetime
{
    private readonly IDbContextFactory<ALODContext> _contextFactory;
    private ALODContext? _context;
    private IDbContextTransaction? _transaction;
    private readonly List<Func<ALODContext, Task>> _cleanupActions = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the TestDataCleanupHelper.
    /// </summary>
    public TestDataCleanupHelper(IDbContextFactory<ALODContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Initializes the helper asynchronously (creates context and optional transaction).
    /// </summary>
    public async Task InitializeAsync()
    {
        _context = await _contextFactory.CreateDbContextAsync();
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Disposes the helper asynchronously (rolls back transaction and disposes context).
    /// </summary>
    public async Task DisposeAsync()
    {
        await Cleanup();
    }

    /// <summary>
    /// Gets the active database context for this helper.
    /// </summary>
    public ALODContext GetContext()
    {
        ThrowIfDisposed();
        return _context ?? throw new InvalidOperationException("Context not initialized");
    }

    /// <summary>
    /// Adds a single entity to the database.
    /// </summary>
    public async Task<T> AddAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        _context!.Add(entity);
        await _context.SaveChangesAsync();

        // Register automatic cleanup
        RegisterCleanup(async ctx => ctx.Set<T>().Remove(entity));

        return entity;
    }

    /// <summary>
    /// Adds multiple entities to the database.
    /// </summary>
    public async Task<List<T>> AddRangeAsync<T>(params T[] entities) where T : class
    {
        ThrowIfDisposed();
        var entityList = entities.ToList();
        _context!.AddRange(entityList);
        await _context.SaveChangesAsync();

        // Register automatic cleanup for all entities
        RegisterCleanup(async ctx =>
        {
            foreach (var entity in entityList)
                ctx.Set<T>().Remove(entity);
            await ctx.SaveChangesAsync();
        });

        return entityList;
    }

    /// <summary>
    /// Adds a custom cleanup action to be executed during disposal.
    /// </summary>
    public void RegisterCleanup(Func<ALODContext, Task> cleanupAction)
    {
        ThrowIfDisposed();
        _cleanupActions.Add(cleanupAction);
    }

    /// <summary>
    /// Manually executes all registered cleanup actions.
    /// </summary>
    public async Task ExecuteCleanup()
    {
        ThrowIfDisposed();

        if (_context == null)
            return;

        foreach (var action in _cleanupActions)
        {
            try
            {
                await action(_context);
            }
            catch (Exception ex)
            {
                // Log cleanup exceptions but continue with other cleanup actions
                System.Diagnostics.Debug.WriteLine($"Cleanup action failed: {ex.Message}");
            }
        }

        _cleanupActions.Clear();
    }

    /// <summary>
    /// Rolls back the current transaction without disposing context.
    /// </summary>
    public async Task RollbackTransaction()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <summary>
    /// Commits the current transaction and starts a new one.
    /// </summary>
    public async Task CommitAndStartNewTransaction()
    {
        ThrowIfDisposed();

        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
        }

        _transaction = await _context!.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Clears all data from a specific table.
    /// </summary>
    public async Task ClearTable<T>() where T : class
    {
        ThrowIfDisposed();
        var allEntities = await _context!.Set<T>().ToListAsync();
        _context.RemoveRange(allEntities);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Checks if an entity exists in the database.
    /// </summary>
    public async Task<bool> ExistsAsync<T>(Func<IQueryable<T>, IQueryable<T>> predicate) where T : class
    {
        ThrowIfDisposed();
        var query = predicate(_context!.Set<T>());
        return await query.AnyAsync();
    }

    /// <summary>
    /// Retrieves an entity by predicate.
    /// </summary>
    public async Task<T?> FindAsync<T>(Func<IQueryable<T>, IQueryable<T>> predicate) where T : class
    {
        ThrowIfDisposed();
        var query = predicate(_context!.Set<T>());
        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Retrieves all entities matching a predicate.
    /// </summary>
    public async Task<List<T>> FindAllAsync<T>(Func<IQueryable<T>, IQueryable<T>> predicate) where T : class
    {
        ThrowIfDisposed();
        var query = predicate(_context!.Set<T>());
        return await query.ToListAsync();
    }

    /// <summary>
    /// Detaches an entity from change tracking.
    /// </summary>
    public void Detach<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        _context!.Entry(entity).State = EntityState.Detached;
    }

    /// <summary>
    /// Refreshes an entity from the database.
    /// </summary>
    public async Task<T> RefreshAsync<T>(T entity) where T : class
    {
        ThrowIfDisposed();
        await _context!.Entry(entity).ReloadAsync();
        return entity;
    }

    /// <summary>
    /// Gets the count of entities matching a predicate.
    /// </summary>
    public async Task<int> CountAsync<T>(Func<IQueryable<T>, IQueryable<T>>? predicate = null) where T : class
    {
        ThrowIfDisposed();
        var query = predicate == null ? _context!.Set<T>() : predicate(_context!.Set<T>());
        return await query.CountAsync();
    }

    /// <summary>
    /// Gets transaction state as string.
    /// </summary>
    public string GetTransactionState()
    {
        return _transaction?.GetDbTransaction().Connection?.State.ToString() ?? "No transaction";
    }

    /// <summary>
    /// Internal cleanup logic.
    /// </summary>
    private async Task Cleanup()
    {
        if (_disposed)
            return;

        try
        {
            // Execute all registered cleanup actions
            await ExecuteCleanup();

            // Rollback transaction if still active
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
        }
        finally
        {
            // Dispose context
            if (_context != null)
            {
                await _context.DisposeAsync();
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Throws if helper has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }
}

/// <summary>
/// Factory for creating test data fixtures with isolated database contexts.
/// </summary>
public class TestDataFactory
{
    private readonly IDbContextFactory<ALODContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the TestDataFactory.
    /// </summary>
    public TestDataFactory(IDbContextFactory<ALODContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Creates test helper with isolated context.
    /// </summary>
    public TestDataCleanupHelper CreateHelper()
    {
        return new TestDataCleanupHelper(_contextFactory);
    }
}

/// <summary>
/// Provides transaction scope management for isolated test operations.
/// </summary>
public class TransactionScope : IAsyncDisposable
{
    private readonly IDbContextFactory<ALODContext> _contextFactory;
    private ALODContext? _context;
    private IDbContextTransaction? _transaction;

    /// <summary>
    /// Initializes a new instance of TransactionScope.
    /// </summary>
    public TransactionScope(IDbContextFactory<ALODContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Begins a new transaction asynchronously.
    /// </summary>
    public async Task BeginAsync()
    {
        _context = await _contextFactory.CreateDbContextAsync();
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Gets the context within this transaction scope.
    /// </summary>
    public ALODContext GetContext()
    {
        return _context ?? throw new InvalidOperationException("Transaction not initialized");
    }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
        }
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Asynchronously disposes the scope and rolls back transaction.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }

        if (_context != null)
        {
            await _context.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
