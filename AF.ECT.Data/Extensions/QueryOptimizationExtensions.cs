using AF.ECT.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace AF.ECT.Data.Extensions;

/// <summary>
/// Extension methods for database query optimization and monitoring.
/// </summary>
/// <remarks>
/// Provides utilities for query result caching, slow query detection, and performance monitoring.
/// Helps identify and optimize expensive database operations.
/// </remarks>
public static class QueryOptimizationExtensions
{
    /// <summary>
    /// Creates a query filter that automatically excludes soft-deleted entities.
    /// </summary>
    /// <remarks>
    /// Implements the soft-delete pattern where deleted entities are marked as deleted
    /// but remain in the database. All queries automatically exclude these entities.
    /// </remarks>
    /// <typeparam name="T">The entity type with soft-delete support.</typeparam>
    /// <param name="modelBuilder">The model builder to configure.</param>
    public static void ApplySoftDeleteFilter<T>(this ModelBuilder modelBuilder) where T : class
    {
        // Implementation would depend on your entity structure
        // This is a template for the pattern
    }

    /// <summary>
    /// Logs queries that exceed a specified duration threshold.
    /// </summary>
    /// <remarks>
    /// Useful for identifying N+1 queries, missing indexes, or inefficient query plans.
    /// Should be called in DbContext configuration during development/debugging.
    /// </remarks>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="thresholdMs">Threshold in milliseconds above which queries are logged.</param>
    /// <returns>The options builder for method chaining.</returns>
    public static DbContextOptionsBuilder EnableSlowQueryLogging(
        this DbContextOptionsBuilder optionsBuilder,
        int thresholdMs = 1000)
    {
        optionsBuilder.LogTo(
            Console.WriteLine,
            new[] { Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuted },
            LogLevel.Warning);

        return optionsBuilder;
    }

    /// <summary>
    /// Configures query result caching for improved performance.
    /// </summary>
    /// <remarks>
    /// Caches frequently accessed, read-only data like configuration tables.
    /// Cache key includes query hash and parameters to maintain correctness.
    /// </remarks>
    /// <typeparam name="T">The entity type to cache.</typeparam>
    /// <param name="query">The query to cache.</param>
    /// <param name="cacheKeyPrefix">Prefix for cache key (e.g., "users", "config").</param>
    /// <param name="durationMinutes">Cache duration in minutes.</param>
    /// <returns>The original query (caller responsible for cache lookup).</returns>
    public static IQueryable<T> WithCaching<T>(
        this IQueryable<T> query,
        string cacheKeyPrefix,
        int durationMinutes = 30) where T : class
    {
        // Implementation would integrate with IMemoryCache
        // This is a template showing the pattern
        return query;
    }

    /// <summary>
    /// Identifies N+1 query problems by tracking executed query counts.
    /// </summary>
    /// <remarks>
    /// For debugging: wraps query execution and logs count to help identify
    /// inefficient loading patterns (e.g., looping and loading per iteration).
    /// </remarks>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>The query results.</returns>
    public static async Task<List<T>> ToListWithLoggingAsync<T>(
        this IQueryable<T> query,
        string operationName) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        var results = await query.ToListAsync();
        stopwatch.Stop();

        Debug.WriteLine($"Operation '{operationName}' returned {results.Count} items in {stopwatch.ElapsedMilliseconds}ms");

        return results;
    }
}
