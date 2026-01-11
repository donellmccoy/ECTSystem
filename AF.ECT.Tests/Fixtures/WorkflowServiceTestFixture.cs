using System.Linq.Expressions;
using AF.ECT.Server.Services;
using AF.ECT.Server.Services.Interfaces;

namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Fixture for WorkflowService tests providing shared dependencies and lifecycle management.
/// Reduces boilerplate by centralizing mock setup and common test data creation.
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
    public void VerifyDataServiceCall(Expression<Action<IDataService>> verifyAction)
    {
        MockDataService.Verify(verifyAction, Times.Once);
    }
}
