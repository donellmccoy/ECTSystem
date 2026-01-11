namespace AF.ECT.Tests.Fixtures;

using AF.ECT.Tests.Common;
using System.Collections.Concurrent;

/// <summary>
/// Provides cached test data to avoid regeneration across tests.
/// Implements singleton pattern for shared fixture data across collection.
/// </summary>
public class CachedTestDataFixture : IAsyncLifetime
{
    private static readonly ConcurrentDictionary<string, object?> _dataCache = new();
    private static readonly object _lockObj = new();

    /// <summary>
    /// Gets or creates cached data using a factory function.
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory) where T : notnull
    {
        lock (_lockObj)
        {
            if (_dataCache.TryGetValue(key, out var cached))
            {
                return (T)cached!;
            }

            var data = factory();
            _dataCache.TryAdd(key, data);
            return data;
        }
    }

    /// <summary>
    /// Gets or creates cached data asynchronously.
    /// </summary>
    public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory) where T : notnull
    {
        lock (_lockObj)
        {
            if (_dataCache.TryGetValue(key, out var cached))
            {
                return (T)cached!;
            }
        }

        var data = await factory();
        _dataCache.TryAdd(key, data);
        return data;
    }

    /// <summary>
    /// Gets cached test data - generates large request batches.
    /// </summary>
    public List<GetReinvestigationRequestsRequest> GetRequestBatch(int count)
    {
        return GetOrCreate($"requests_batch_{count}", () =>
        {
            var requests = new List<GetReinvestigationRequestsRequest>();
            for (int i = 0; i < count; i++)
            {
                requests.Add(new GetReinvestigationRequestsRequest
                {
                    UserId = (i % 100) + 1,
                    Sarc = i % 2 == 0
                });
            }
            return requests;
        });
    }

    /// <summary>
    /// Gets cached mock results - generates large response datasets.
    /// </summary>
    public List<core_lod_sp_GetReinvestigationRequestsResult> GetMockResultsBatch(int count)
    {
        return GetOrCreate($"mock_results_{count}", () =>
        {
            var results = new List<core_lod_sp_GetReinvestigationRequestsResult>();
            for (int i = 0; i < count; i++)
            {
                results.Add(new core_lod_sp_GetReinvestigationRequestsResult());
            }
            return results;
        });
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public void ClearCache()
    {
        lock (_lockObj)
        {
            _dataCache.Clear();
        }
    }

    /// <summary>
    /// Fixture initialization - called once per collection.
    /// </summary>
    public Task InitializeAsync()
    {
        // Warm up cache with commonly used data
        _ = GetRequestBatch(10);
        _ = GetRequestBatch(100);
        _ = GetMockResultsBatch(10);
        _ = GetMockResultsBatch(100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fixture cleanup - called once after collection tests complete.
    /// </summary>
    public Task DisposeAsync()
    {
        ClearCache();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Collection definition using cached test data fixture.
/// </summary>
[CollectionDefinition("Cached Test Data")]
public class CachedTestDataCollection : ICollectionFixture<CachedTestDataFixture>
{
    // This class has no code, and is never instantiated. Its purpose is purely
    // to define the collection that tests can join in order to use the cached fixture.
}
