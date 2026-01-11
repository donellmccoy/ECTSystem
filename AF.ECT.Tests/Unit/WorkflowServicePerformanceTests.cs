using AF.ECT.Server.Services;
using AF.ECT.Server.Services.Interfaces;
using AF.ECT.Data.Interfaces;
using AF.ECT.Data.ResultTypes;
using AF.ECT.Tests.Common;
using AF.ECT.Tests.Fixtures;
using FluentAssertions;
using System.Diagnostics;
using Grpc.Core;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Performance benchmark tests for WorkflowService critical paths.
/// Ensures operations complete within acceptable time thresholds under various loads.
/// Uses cached test data to avoid regeneration across tests.
/// </summary>
[Collection("Cached Test Data")]
[Trait("Category", "Unit")]
[Trait("Component", "WorkflowService Performance")]
public class WorkflowServicePerformanceTests
{
    private readonly Mock<ILogger<WorkflowServiceImpl>> _mockLogger;
    private readonly Mock<IDataService> _mockDataService;
    private readonly CachedTestDataFixture _cachedFixture;
    private const int PerformanceThresholdMs = 500;

    public WorkflowServicePerformanceTests(CachedTestDataFixture cachedFixture)
    {
        _mockLogger = new Mock<ILogger<WorkflowServiceImpl>>();
        _mockDataService = new Mock<IDataService>();
        _cachedFixture = cachedFixture;
    }

    private WorkflowServiceImpl CreateService() =>
        new(_mockLogger.Object, _mockDataService.Object, new TestResilienceService());

    private static ServerCallContext CreateMockServerCallContext()
    {
        var mockContext = new Mock<ServerCallContext>(MockBehavior.Loose);
        return mockContext.Object;
    }

    #region Single Item Operations

    /// <summary>
    /// Benchmark: GetReinvestigationRequests with single item should complete fast.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_SingleItem_CompletesWithinThreshold()
    {
        // Arrange
        var mockResults = _cachedFixture.GetMockResultsBatch(1);
        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await service.GetReinvestigationRequests(request, CreateMockServerCallContext());
        stopwatch.Stop();

        // Assert
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(PerformanceThresholdMs);
    }

    #endregion

    #region Moderate Load Operations

    /// <summary>
    /// Benchmark: GetReinvestigationRequests with 100 items should complete reasonably.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_HundredItems_CompletesWithinThreshold()
    {
        // Arrange
        var mockResults = _cachedFixture.GetMockResultsBatch(100);

        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await service.GetReinvestigationRequests(request, CreateMockServerCallContext());
        stopwatch.Stop();

        // Assert
        response.Items.Should().HaveCount(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(PerformanceThresholdMs);
    }

    #endregion

    #region Stress Test Operations

    /// <summary>
    /// Benchmark: Multiple concurrent requests should not degrade performance significantly.
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ConcurrentLoad_MaintainsAveragePerformance()
    {
        // Arrange
        var mockResults = Enumerable.Range(0, 10)
            .Select(_ => new core_lod_sp_GetReinvestigationRequestsResult())
            .ToList();

        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        var stopwatch = Stopwatch.StartNew();
        var concurrencyLevel = 5;

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel)
            .Select(async _ => await service.GetReinvestigationRequests(request, CreateMockServerCallContext()))
            .ToList();

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(concurrencyLevel);
        var averageTimeMs = stopwatch.ElapsedMilliseconds / (double)concurrencyLevel;
        averageTimeMs.Should().BeLessThan(PerformanceThresholdMs);
    }

    #endregion

    #region Memory Efficiency Tests

    /// <summary>
    /// Benchmark: Large dataset handling should not cause excessive memory growth.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_LargeDataset_MemoryEfficient()
    {
        // Arrange
        var largeDataset = _cachedFixture.GetMockResultsBatch(1000);

        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(largeDataset);

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        // Force garbage collection before test
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var memoryBefore = GC.GetTotalMemory(false);

        // Act
        var response = await service.GetReinvestigationRequests(request, CreateMockServerCallContext());

        var memoryAfter = GC.GetTotalMemory(false);
        var memoryUsedMb = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        // Assert
        response.Items.Should().HaveCount(1000);
        // Memory usage should be reasonable (less than 50MB for 1000 items)
        memoryUsedMb.Should().BeLessThan(50);
    }

    #endregion

    #region Data Mapping Performance

    /// <summary>
    /// Benchmark: Response mapping should not add significant overhead.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task GetActionsByStep_ResponseMapping_MeasuresOverhead(int itemCount)
    {
        // Arrange
        var mockResults = Enumerable.Range(0, itemCount)
            .Select(i => new core_workflow_sp_GetActionsByStepResult
            {
                wso_id = i,
                wsa_id = i,
                actionType = 1,
                target = i,
                data = null,
                text = $"Action {i}"
            })
            .ToList();

        _mockDataService.Setup(ds => ds.GetActionsByStepAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        var service = CreateService();
        var request = new GetActionsByStepRequest { StepId = 1 };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await service.GetActionsByStep(request, CreateMockServerCallContext());
        stopwatch.Stop();

        // Assert
        response.Items.Should().HaveCount(itemCount);
        // Mapping should be sub-linear in time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(PerformanceThresholdMs * itemCount / 100);
    }

    #endregion
}
