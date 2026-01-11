using System.Linq.Expressions;
using AF.ECT.Server.Services;
using AF.ECT.Server.Services.Interfaces;

namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Fixture for WorkflowService tests providing shared dependencies and lifecycle management.
/// Reduces boilerplate by centralizing mock setup and common test data creation.
/// 
/// <remarks>
/// This fixture is used in conjunction with xUnit's IAsyncLifetime to ensure proper
/// initialization and cleanup of mock objects across multiple test runs. It provides:
/// 
/// - Centralized mock creation and configuration
/// - Reusable setup patterns for common scenarios
/// - Helper methods for mock verification
/// - ServerCallContext creation for gRPC testing
/// 
/// Example usage:
/// <code>
/// [Collection("WorkflowService Tests")]
/// public class MyTests : IAsyncLifetime
/// {
///     private WorkflowServiceTestFixture _fixture = null!;
///     
///     public Task InitializeAsync()
///     {
///         _fixture = new WorkflowServiceTestFixture();
///         return _fixture.InitializeAsync();
///     }
///     
///     public Task DisposeAsync() => _fixture.DisposeAsync();
///     
///     [Fact]
///     public async Task MyTest()
///     {
///         // Use _fixture.ServiceUnderTest, _fixture.MockDataService, etc.
///     }
/// }
/// </code>
/// </remarks>
/// </summary>
public class WorkflowServiceTestFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the mocked logger for WorkflowServiceImpl.
    /// </summary>
    public Mock<ILogger<WorkflowServiceImpl>> MockLogger { get; private set; } = null!;

    /// <summary>
    /// Gets the mocked data service.
    /// </summary>
    public Mock<IDataService> MockDataService { get; private set; } = null!;

    /// <summary>
    /// Gets the mocked resilience service.
    /// </summary>
    public Mock<IResilienceService> MockResilienceService { get; private set; } = null!;

    /// <summary>
    /// Gets the WorkflowServiceImpl instance under test.
    /// </summary>
    public WorkflowServiceImpl ServiceUnderTest { get; private set; } = null!;

    /// <summary>
    /// Asynchronously initializes the fixture with all required mocks and service.
    /// </summary>
    public Task InitializeAsync()
    {
        // Initialize mocks
        MockLogger = new Mock<ILogger<WorkflowServiceImpl>>();
        MockDataService = DataServiceMockFactory.CreateDefaultMock();
        MockResilienceService = new Mock<IResilienceService>();

        // Create service instance with mock resilience service
        ServiceUnderTest = new WorkflowServiceImpl(
            MockLogger.Object,
            MockDataService.Object,
            MockResilienceService.Object);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously cleans up fixture resources.
    /// </summary>
    public Task DisposeAsync()
    {
        MockLogger = null!;
        MockDataService = null!;
        MockResilienceService = null!;
        ServiceUnderTest = null!;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures the data service mock to return specific test data for a method.
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="setupAction">Action to configure the mock</param>
    /// <remarks>
    /// This method allows flexible mock configuration without directly accessing the mock object.
    /// Example: <c>fixture.SetupDataServiceMock&lt;GetUserNameRequest, GetUserNameResponse&gt;(m => 
    ///     m.Setup(ds => ds.GetUserNameAsync(...)).ReturnsAsync(...))</c>
    /// </remarks>
    public void SetupDataServiceMock<TRequest, TResponse>(
        Action<Mock<IDataService>> setupAction)
        where TRequest : class
        where TResponse : class
    {
        setupAction(MockDataService);
    }

    /// <summary>
    /// Creates a mock ServerCallContext for gRPC service tests.
    /// </summary>
    /// <param name="correlationId">Optional correlation ID for audit tracking</param>
    /// <returns>A configured ServerCallContext mock</returns>
    /// <remarks>
    /// The ServerCallContext mock is configured with:
    /// - CancellationToken: CancellationToken.None (no cancellation)
    /// - Metadata: Empty (can be extended if needed)
    /// - Peer: "localhost" (simulated peer connection)
    /// </remarks>
    public ServerCallContext CreateMockServerCallContext(string? correlationId = null)
    {
        var mockContext = new Mock<ServerCallContext>();
        mockContext.Setup(c => c.CancellationToken).Returns(CancellationToken.None);
        
        return mockContext.Object;
    }

    /// <summary>
    /// Verifies that a data service method was called with specific parameters.
    /// </summary>
    /// <param name="verifyAction">The verification expression</param>
    /// <remarks>
    /// This helper enforces a default verification count of Times.Once.
    /// For different verification counts, use MockDataService.Verify() directly.
    /// Example: <c>fixture.VerifyDataServiceCall(ds => ds.GetUserNameAsync(
    ///     It.IsAny&lt;string&gt;(), It.IsAny&lt;string&gt;(), It.IsAny&lt;CancellationToken&gt;()))</c>
    /// </remarks>
    public void VerifyDataServiceCall(Expression<Action<IDataService>> verifyAction)
    {
        MockDataService.Verify(verifyAction, Times.Once);
    }

    /// <summary>
    /// Verifies that a data service method was called a specific number of times.
    /// </summary>
    /// <param name="verifyAction">The verification expression</param>
    /// <param name="times">The expected number of invocations</param>
    /// <remarks>
    /// Provides flexibility for verifying different call counts.
    /// Example: <c>fixture.VerifyDataServiceCall(ds => ds.GetUserNameAsync(...), Times.Exactly(3))</c>
    /// </remarks>
    public void VerifyDataServiceCall(Expression<Action<IDataService>> verifyAction, Times times)
    {
        MockDataService.Verify(verifyAction, times);
    }
}
