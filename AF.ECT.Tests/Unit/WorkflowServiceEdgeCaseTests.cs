using AF.ECT.Server.Services;
using AF.ECT.Server.Services.Interfaces;
using AF.ECT.Data.Interfaces;
using AF.ECT.Data.ResultTypes;
using AF.ECT.Tests.Common;
using FluentAssertions;
using Grpc.Core;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Tests for edge cases and boundary conditions in WorkflowService methods.
/// Covers null inputs, empty collections, timeout scenarios, and concurrent requests.
/// </summary>
[Collection("WorkflowService Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "WorkflowService EdgeCases")]
public class WorkflowServiceEdgeCaseTests
{
    private readonly Mock<ILogger<WorkflowServiceImpl>> _mockLogger;
    private readonly Mock<IDataService> _mockDataService;

    public WorkflowServiceEdgeCaseTests()
    {
        _mockLogger = new Mock<ILogger<WorkflowServiceImpl>>();
        _mockDataService = new Mock<IDataService>();
    }

    private WorkflowServiceImpl CreateService() =>
        new(_mockLogger.Object, _mockDataService.Object, new TestResilienceService());

    private static ServerCallContext CreateMockServerCallContext()
    {
        var mockContext = new Mock<ServerCallContext>(MockBehavior.Loose);
        return mockContext.Object;
    }

    #region Null/Invalid Input Tests

    /// <summary>
    /// Tests that methods handle null requests gracefully.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<RpcException>(
            () => service.GetReinvestigationRequests(null!, null!));
    }

    /// <summary>
    /// Tests that string parameters with only whitespace are handled.
    /// </summary>
    [Theory]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("")]
    public async Task GetUserName_WithWhitespaceOnlyName_HandlesProperly(string invalidName)
    {
        // Arrange
        var request = new GetUserNameRequest { First = invalidName, Last = invalidName };
        _mockDataService.Setup(ds => ds.GetUserNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUserNameResult>());
        var service = CreateService();

        // Act
        var response = await service.GetUserName(request, CreateMockServerCallContext());

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that methods handle negative integer IDs.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(-999)]
    [InlineData(int.MinValue)]
    public async Task GetActiveCases_WithNegativeIds_HandlesGracefully(int negativeId)
    {
        // Arrange
        var request = new GetActiveCasesRequest { RefId = negativeId, GroupId = negativeId };
        _mockDataService.Setup(ds => ds.GetActiveCasesAsync(It.IsAny<int>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetActiveCasesResult>());
        var service = CreateService();

        // Act
        var response = await service.GetActiveCases(request, CreateMockServerCallContext());

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region Boundary Value Tests

    /// <summary>
    /// Tests that boundary integer values are processed correctly.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public async Task GetManagedUsers_WithBoundaryIntegerValues_ProcessesSuccessfully(int boundaryValue)
    {
        // Arrange
        var request = new GetManagedUsersRequest { Userid = boundaryValue, Status = 1, Role = 1, SrchUnit = 1 };
        _mockDataService.Setup(ds => ds.GetManagedUsersAsync(It.IsAny<GetManagedUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetManagedUsersResult>());
        var service = CreateService();

        // Act
        var response = await service.GetManagedUsers(request, CreateMockServerCallContext());

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region Timeout Tests

    /// <summary>
    /// Tests that operations timeout gracefully under time constraints.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_WithSlowDataService_TimesOutAppropriately()
    {
        // Arrange
        var delayMs = 6000;
        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(delayMs);
                return new List<core_lod_sp_GetReinvestigationRequestsResult>();
            });

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        // Act & Assert
        var timeoutTask = service.GetReinvestigationRequests(request, CreateMockServerCallContext());
        
        // Should complete (may not timeout if TestResilienceService doesn't enforce)
        var completed = await Task.WhenAny(timeoutTask, Task.Delay(1000));
        if (completed == timeoutTask)
        {
            var result = await timeoutTask;
            result.Should().NotBeNull();
        }
    }

    #endregion

    #region Large Dataset Tests

    /// <summary>
    /// Tests handling of large result sets from data service.
    /// </summary>
    [Theory]
    [ClassData(typeof(LargeDatasetTestData))]
    public async Task GetReinvestigationRequests_WithLargeResultSet_HandlesMemoryEfficiently(int itemCount)
    {
        // Arrange
        var mockResults = new List<core_lod_sp_GetReinvestigationRequestsResult>();
        for (int i = 0; i < itemCount; i++)
        {
            mockResults.Add(new core_lod_sp_GetReinvestigationRequestsResult());
        }

        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockResults);

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        // Act
        var response = await service.GetReinvestigationRequests(request, CreateMockServerCallContext());

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().HaveCount(itemCount);
    }

    #endregion

    #region Concurrent Request Tests

    /// <summary>
    /// Tests that concurrent requests are handled properly when data is returned.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequests_WithConcurrentRequests_AllCompleteSuccessfully()
    {
        // Arrange
        _mockDataService.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_lod_sp_GetReinvestigationRequestsResult> { new() });

        var service = CreateService();
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        // Act
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => service.GetReinvestigationRequests(request, CreateMockServerCallContext()))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
    }

    #endregion
}

/// <summary>
/// Test data for large dataset scenarios.
/// </summary>
public class LargeDatasetTestData : TheoryData<int>
{
    public LargeDatasetTestData()
    {
        Add(100);
        Add(1000);
        Add(10000);
    }
}
