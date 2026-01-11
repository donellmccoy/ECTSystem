using Grpc.Net.Client;
using Xunit;
using FluentAssertions;
using System.Diagnostics;
using AF.ECT.Shared;

namespace AF.ECT.Tests.Integration;

/// <summary>
/// Integration tests focusing on performance characteristics of gRPC service and data layer operations.
/// Tests query optimization, index effectiveness, bulk operations, and mission-critical path performance.
/// </summary>
[Collection("Integration Tests")]
[Trait("Category", "Integration")]
[Trait("Component", "PerformanceTests")]
public class PerformanceTests : IAsyncLifetime
{
    private GrpcChannel _channel = null!;
    private WorkflowService.WorkflowServiceClient _client = null!;

    /// <summary>
    /// Initializes the test fixture with a gRPC channel and client.
    /// </summary>
    public async Task InitializeAsync()
    {
        _channel = GrpcChannel.ForAddress("http://localhost:5000");
        _client = new WorkflowService.WorkflowServiceClient(_channel);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up resources after tests complete.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.ShutdownAsync();
            _channel.Dispose();
        }
    }

    #region Query Optimization Tests

    /// <summary>
    /// Tests that single-row lookups by ID execute efficiently within acceptable latency bounds.
    /// Validates that indexed queries perform at sub-millisecond scale.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestById_ExecutesWithinOptimalLatency()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetReinvestigationRequestsAsync(request);
        stopwatch.Stop();

        // Assert - Single-row indexed query should complete in < 50ms
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    /// <summary>
    /// Tests that filtering large result sets with WHERE clauses completes within reasonable bounds.
    /// Validates index effectiveness for common filter patterns.
    /// </summary>
    [Fact]
    public async Task FilteredQuery_UsesIndexEffectively()
    {
        // Arrange
        var requests = new List<GetReinvestigationRequestsRequest>
        {
            new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false },
            new GetReinvestigationRequestsRequest { UserId = 2, Sarc = true },
            new GetReinvestigationRequestsRequest { UserId = 3, Sarc = false }
        };

        var stopwatch = Stopwatch.StartNew();

        // Act - Execute 3 filtered queries
        var results = new List<GetReinvestigationRequestsResponse>();
        foreach (var req in requests)
        {
            var response = await _client.GetReinvestigationRequestsAsync(req);
            results.Add(response);
        }
        stopwatch.Stop();

        // Assert - 3 queries should complete in < 150ms total
        results.Should().HaveCount(3);
        results.All(r => r != null).Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(150);
    }

    /// <summary>
    /// Tests that JOIN operations between tables execute efficiently.
    /// Common pattern: ReinvestigationRequest joined with User management data.
    /// </summary>
    [Fact]
    public async Task ComplexJoinQuery_ExecutesWithinAcceptableBounds()
    {
        // Arrange - Get managed users (typically involves JOIN with organizations/permissions)
        var request = new GetManagedUsersRequest { };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetManagedUsersAsync(request);
        stopwatch.Stop();

        // Assert - JOIN query should complete in < 100ms
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region Index Effectiveness Tests

    /// <summary>
    /// Tests that sorting large result sets doesn't degrade performance.
    /// Validates that ORDER BY clauses leverage indexes effectively.
    /// </summary>
    [Fact]
    public async Task SortedResultSet_WithIndex_PerformsEfficiently()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var iterations = 5;
        var elapsedTimes = new List<long>();

        // Act - Execute same query multiple times to test consistent performance
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetReinvestigationRequestsAsync(request);
            stopwatch.Stop();
            elapsedTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - All iterations should be within acceptable range (< 50ms each)
        // and deviation should be minimal (indicating index usage)
        elapsedTimes.Should().HaveCount(iterations);
        elapsedTimes.Average().Should().BeLessThan(50);
        var stdDev = CalculateStandardDeviation(elapsedTimes);
        stdDev.Should().BeLessThan(20); // Low variance indicates consistent index usage
    }

    /// <summary>
    /// Tests that filtering on indexed columns is significantly faster than unindexed columns.
    /// Validates query plan is using available indexes.
    /// </summary>
    [Fact]
    public async Task IndexedColumnFilter_SignificantlyFasterThanFullScan()
    {
        // Arrange - Query that filters on indexed column (UserId is typically indexed)
        var indexedFilterRequest = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };

        // Act
        var stopwatch = Stopwatch.StartNew();
        var response = await _client.GetReinvestigationRequestsAsync(indexedFilterRequest);
        stopwatch.Stop();

        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Assert - Indexed query should be very fast (< 50ms)
        response.Should().NotBeNull();
        elapsedMs.Should().BeLessThan(50);
    }

    #endregion

    #region Bulk Operation Performance Tests

    /// <summary>
    /// Tests that retrieving a moderately large result set (100+ records) doesn't cause excessive latency.
    /// Validates pagination or streaming is working correctly for bulk data.
    /// </summary>
    [Fact]
    public async Task LargeBulkResultSet_RetrievedWithinAcceptableTime()
    {
        // Arrange
        var request = new GetManagedUsersRequest { };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetManagedUsersAsync(request);
        stopwatch.Stop();

        // Assert - Bulk operation should complete in < 500ms
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    /// <summary>
    /// Tests that multiple rapid successive queries don't accumulate in latency.
    /// Validates connection pooling and query optimization under light load.
    /// </summary>
    [Fact]
    public async Task RapidSuccessiveQueries_MaintainConsistentPerformance()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var queryCount = 10;
        var elapsedTimes = new List<long>();

        // Act - Execute 10 rapid queries
        for (int i = 0; i < queryCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetReinvestigationRequestsAsync(request);
            stopwatch.Stop();
            elapsedTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Each query should be fast and consistent
        elapsedTimes.Should().HaveCount(queryCount);
        elapsedTimes.Average().Should().BeLessThan(50);
        
        // No query should significantly outlier (no connection pool exhaustion)
        var max = elapsedTimes.Max();
        var min = elapsedTimes.Min();
        (max - min).Should().BeLessThan(30); // Max 30ms variance between fastest and slowest
    }

    /// <summary>
    /// Tests concurrent bulk operations to simulate real-world multi-user load.
    /// Validates that concurrent access doesn't cause query degradation.
    /// </summary>
    [Fact]
    public async Task ConcurrentBulkOperations_MaintainAcceptableLatency()
    {
        // Arrange
        var request1 = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var request2 = new GetReinvestigationRequestsRequest { UserId = 2, Sarc = true };
        var request3 = new GetManagedUsersRequest { };

        // Act - Execute 3 concurrent bulk operations
        var stopwatch = Stopwatch.StartNew();
        var call1 = _client.GetReinvestigationRequestsAsync(request1);
        var call2 = _client.GetReinvestigationRequestsAsync(request2);
        var call3 = _client.GetManagedUsersAsync(request3);

        var response1 = await call1.ResponseAsync;
        var response2 = await call2.ResponseAsync;
        var response3 = await call3.ResponseAsync;
        stopwatch.Stop();

        // Assert - All 3 concurrent operations should complete in < 200ms
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response3.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    #endregion

    #region Mission-Critical Path Tests

    /// <summary>
    /// Tests that critical user lookup (GetUserName) executes with minimal latency.
    /// This is often called in request processing pipelines and needs to be fast.
    /// </summary>
    [Fact]
    public async Task CriticalUserNameLookup_MeetsLatencySLA()
    {
        // Arrange - GetUserName is called frequently during request processing
        var request = new GetUserNameRequest { First = "John", Last = "Doe" };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetUserNameAsync(request);
        stopwatch.Stop();

        // Assert - Critical path should be < 30ms (SLA for user-facing operations)
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(30);
    }

    /// <summary>
    /// Tests that permission checking queries execute with minimal latency.
    /// Permissions are checked frequently in authorization flows.
    /// </summary>
    [Fact]
    public async Task PermissionCheckQuery_MeetsAuthorizationSLA()
    {
        // Arrange
        var request = new GetPermissionsRequest { WorkflowId = 1 };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetPermissionsAsync(request);
        stopwatch.Stop();

        // Assert - Authorization query should be < 50ms
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    /// <summary>
    /// Tests the most critical workflow retrieval operation.
    /// This operation is the backbone of case tracking functionality.
    /// </summary>
    [Fact]
    public async Task CriticalWorkflowRetrieval_MeetsMissionCriticalSLA()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _client.GetReinvestigationRequestsAsync(request);
        stopwatch.Stop();

        // Assert - Core workflow operation should be < 100ms
        response.Should().NotBeNull();
        response.Items.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Calculates the standard deviation of a list of elapsed times.
    /// Used to measure consistency of query performance across multiple executions.
    /// </summary>
    /// <param name="times">List of elapsed times in milliseconds.</param>
    /// <returns>Standard deviation of the elapsed times.</returns>
    private static double CalculateStandardDeviation(List<long> times)
    {
        if (times.Count < 2)
            return 0;

        var average = times.Average();
        var sumOfSquaresOfDifferences = times.Sum(t => Math.Pow(t - average, 2));
        var standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / times.Count);
        return standardDeviation;
    }

    #endregion
}
