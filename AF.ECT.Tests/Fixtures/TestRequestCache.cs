namespace AF.ECT.Tests.Fixtures;

using System.Collections.Concurrent;

/// <summary>
/// Provides cached, reusable test request objects to reduce allocation overhead.
/// Caches frequently-used request configurations for efficient test execution.
/// Thread-safe implementation using ConcurrentDictionary.
/// </summary>
public class TestRequestCache : IAsyncLifetime
{
    private static readonly ConcurrentDictionary<string, object> _requestCache = new();

    /// <summary>
    /// Gets or creates a cached GetReinvestigationRequestsRequest with default values.
    /// </summary>
    public GetReinvestigationRequestsRequest GetDefaultReinvestigationRequest()
    {
        const string key = "reinvestigation_default";
        var request = _requestCache.GetOrAdd(key, _ => new GetReinvestigationRequestsRequest
        {
            UserId = 1,
            Sarc = true
        });
        return (GetReinvestigationRequestsRequest)request;
    }

    /// <summary>
    /// Gets or creates a cached GetUserNameRequest with default values.
    /// </summary>
    public GetUserNameRequest GetDefaultUserNameRequest()
    {
        const string key = "username_default";
        var request = _requestCache.GetOrAdd(key, _ => new GetUserNameRequest
        {
            First = "John",
            Last = "Doe"
        });
        return (GetUserNameRequest)request;
    }

    /// <summary>
    /// Gets or creates a cached GetManagedUsersRequest with default values.
    /// </summary>
    public GetManagedUsersRequest GetDefaultManagedUsersRequest()
    {
        const string key = "managedusers_default";
        var request = _requestCache.GetOrAdd(key, _ => new GetManagedUsersRequest
        {
            Userid = 1,
            Ssn = "123456789",
            Name = "John Doe",
            Status = 1,
            Role = 1,
            SrchUnit = 1,
            ShowAllUsers = true
        });
        return (GetManagedUsersRequest)request;
    }

    /// <summary>
    /// Gets or creates a custom cached request with a specific key.
    /// </summary>
    public T GetOrCreateCachedRequest<T>(string key, Func<T> factory) where T : class
    {
        var cacheKey = $"custom_{typeof(T).FullName}_{key}";
        var request = _requestCache.GetOrAdd(cacheKey, _ => factory());
        return (T)request;
    }

    /// <summary>
    /// Clears the request cache. Called during fixture disposal.
    /// </summary>
    public void ClearCache()
    {
        _requestCache.Clear();
    }

    /// <summary>
    /// Initializes the fixture and pre-populates common requests.
    /// </summary>
    public Task InitializeAsync()
    {
        _ = GetDefaultReinvestigationRequest();
        _ = GetDefaultUserNameRequest();
        _ = GetDefaultManagedUsersRequest();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the fixture and clears the cache.
    /// </summary>
    public Task DisposeAsync()
    {
        ClearCache();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Collection definition for tests using TestRequestCache.
/// </summary>
[CollectionDefinition("Test Request Cache")]
public class TestRequestCacheCollection : ICollectionFixture<TestRequestCache>
{
    // This class has no code, and is never instantiated. Its purpose is purely
    // to define the collection that tests can join in order to use the cached request fixture.
}
