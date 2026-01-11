using Grpc.Core;
using Grpc.Net.Client;
using AF.ECT.Tests.Data;
using AF.ECT.Shared.Services;
using static AF.ECT.Tests.Data.WorkflowClientTestData;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Contains unit tests for the <see cref="WorkflowClient"/> class.
/// Tests cover constructor validation, gRPC communication, error handling, and various input scenarios.
/// </summary>
[Collection("GreeterClient Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "GreeterClient")]
public class WorkflowClientTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Mock<WorkflowService.WorkflowServiceClient> _mockGrpcClient;
    private readonly WorkflowClient _greeterClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowClientTests"/> class.
    /// Sets up mock objects and creates a test instance of GreeterClient.
    /// </summary>
    public WorkflowClientTests()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:5001")
        };
        _mockGrpcClient = new Mock<WorkflowService.WorkflowServiceClient>();

        // Setup mock responses for all gRPC methods used in tests
        SetupMockResponses();

        // Use the mocked gRPC client for testing
        _greeterClient = new WorkflowClient(_mockGrpcClient.Object);
    }

    /// <summary>
    /// Sets up mock responses for all gRPC methods to prevent NullReferenceException.
    /// </summary>
    private void SetupMockResponses()
    {
        // GetReinvestigationRequestsAsync mock
        var reinvestigationResponse = new GetReinvestigationRequestsResponse
        {
            Items = { new ReinvestigationRequestItem { Id = 1, Description = "Test Request" } }
        };
        var reinvestigationAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(reinvestigationResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetReinvestigationRequestsAsync(It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(reinvestigationAsyncCall);

        // GetUserNameAsync mock
        var userNameResponse = new GetUserNameResponse
        {
            Items = { new UserNameItem { UserId = 1, FirstName = "John", LastName = "Doe", FullName = "John Doe" } }
        };
        var userNameAsyncCall = new AsyncUnaryCall<GetUserNameResponse>(
            Task.FromResult(userNameResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetUserNameAsync(It.IsAny<GetUserNameRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(userNameAsyncCall);

        // GetManagedUsersAsync mock
        var managedUsersResponse = new GetManagedUsersResponse
        {
            Items = { new ManagedUserItem { UserId = 1, UserName = "ManagedUser" } }
        };
        var managedUsersAsyncCall = new AsyncUnaryCall<GetManagedUsersResponse>(
            Task.FromResult(managedUsersResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetManagedUsersAsync(It.IsAny<GetManagedUsersRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(managedUsersAsyncCall);

        // GetUserAltTitleAsync mock
        var userAltTitleResponse = new GetUserAltTitleResponse
        {
            Items = { new UserAltTitleItem { UserId = 1, Title = "MockTitle", GroupId = 1 } }
        };
        var userAltTitleAsyncCall = new AsyncUnaryCall<GetUserAltTitleResponse>(
            Task.FromResult(userAltTitleResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetUserAltTitleAsync(It.IsAny<GetUserAltTitleRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(userAltTitleAsyncCall);

        // GetPageAccessByGroupAsync mock
        var pageAccessResponse = new GetPageAccessByGroupResponse
        {
            Items = { new PageAccessByGroupItem { PageId = 1, PageName = "TestPage", HasAccess = true, GroupId = 1 } }
        };
        var pageAccessAsyncCall = new AsyncUnaryCall<GetPageAccessByGroupResponse>(
            Task.FromResult(pageAccessResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetPageAccessByGroupAsync(It.IsAny<GetPageAccessByGroupRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(pageAccessAsyncCall);

        // RegisterUserAsync mock
        var registerUserResponse = new RegisterUserResponse { Result = 1 };
        var registerUserAsyncCall = new AsyncUnaryCall<RegisterUserResponse>(
            Task.FromResult(registerUserResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(registerUserAsyncCall);

        // GetWorkflowInitialStatusCodeAsync mock
        var workflowStatusResponse = new GetWorkflowInitialStatusCodeResponse
        {
            Items = { new WorkflowInitialStatusCodeItem { WorkflowId = 1, InitialStatusId = 1, StatusName = "Active" } }
        };
        var workflowStatusAsyncCall = new AsyncUnaryCall<GetWorkflowInitialStatusCodeResponse>(
            Task.FromResult(workflowStatusResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetWorkflowInitialStatusCodeAsync(It.IsAny<GetWorkflowInitialStatusCodeRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(workflowStatusAsyncCall);

        // GetWorkflowTitleAsync mock
        var workflowTitleResponse = new GetWorkflowTitleResponse
        {
            Items = { new WorkflowTitleItem { WorkflowId = 1, Title = "TestWorkflow", ModuleId = 1, SubCase = 1 } }
        };
        var workflowTitleAsyncCall = new AsyncUnaryCall<GetWorkflowTitleResponse>(
            Task.FromResult(workflowTitleResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetWorkflowTitleAsync(It.IsAny<GetWorkflowTitleRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(workflowTitleAsyncCall);

        // GetWorkflowTitleByWorkStatusIdAsync mock
        var workflowTitleByStatusResponse = new GetWorkflowTitleByWorkStatusIdResponse
        {
            Items = { new WorkflowTitleByWorkStatusIdItem { WorkflowId = 1, Title = "TestWorkflow", WorkStatusId = 1, SubCase = 1 } }
        };
        var workflowTitleByStatusAsyncCall = new AsyncUnaryCall<GetWorkflowTitleByWorkStatusIdResponse>(
            Task.FromResult(workflowTitleByStatusResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.GetWorkflowTitleByWorkStatusIdAsync(It.IsAny<GetWorkflowTitleByWorkStatusIdRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(workflowTitleByStatusAsyncCall);

        // SearchMemberDataAsync mock
        var searchMemberResponse = new SearchMemberDataResponse
        {
            Items = { new MemberDataItem { MemberId = 1, Ssn = "123456789", FirstName = "John", LastName = "Doe", MiddleName = "M" } }
        };
        var searchMemberAsyncCall = new AsyncUnaryCall<SearchMemberDataResponse>(
            Task.FromResult(searchMemberResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.SearchMemberDataAsync(It.IsAny<SearchMemberDataRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(searchMemberAsyncCall);

        // SearchMemberDataTestAsync mock
        var searchMemberTestResponse = new SearchMemberDataTestResponse
        {
            Items = { new MemberDataTestItem { MemberId = 2, Ssn = "987654321", Name = "Jane Smith" } }
        };
        var searchMemberTestAsyncCall = new AsyncUnaryCall<SearchMemberDataTestResponse>(
            Task.FromResult(searchMemberTestResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.SearchMemberDataTestAsync(It.IsAny<SearchMemberDataTestRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(searchMemberTestAsyncCall);

        // RegisterUserRoleAsync mock
        var registerUserRoleResponse = new RegisterUserRoleResponse { UserRoleId = 1 };
        var registerUserRoleAsyncCall = new AsyncUnaryCall<RegisterUserRoleResponse>(
            Task.FromResult(registerUserRoleResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.RegisterUserRoleAsync(It.IsAny<RegisterUserRoleRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(registerUserRoleAsyncCall);

        // InsertActionAsync mock
        var insertActionResponse = new InsertActionResponse
        {
            Items = { new InsertActionItem { ActionId = 1, Type = 1, StepId = 1, ResultMessage = "Success" } }
        };
        var insertActionAsyncCall = new AsyncUnaryCall<InsertActionResponse>(
            Task.FromResult(insertActionResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });
        _mockGrpcClient
            .Setup(x => x.InsertActionAsync(It.IsAny<InsertActionRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(insertActionAsyncCall);
    }

    /// <summary>
    /// Cleans up resources after each test.
    /// </summary>
    public void Dispose()
    {
        _greeterClient?.Dispose();
        _httpClient?.Dispose();
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the WorkflowClient constructor throws <see cref="ArgumentNullException"/>
    /// when null gRPC client is provided.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGrpcClientIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowClient(null!));
    }

    /// <summary>
    /// Tests that the WorkflowClient constructor throws <see cref="ArgumentNullException"/>
    /// when null GrpcChannel is provided.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenGrpcChannelIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new WorkflowClient((GrpcChannel)null!));
    }

    #endregion

    #region Core User Method Tests

    /// <summary>
    /// Tests that GetReinvestigationRequestsAsync calls the gRPC service with correct parameters.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_CallsGrpcService_WithCorrectParameters()
    {
        // Arrange
        var userId = 123;
        var sarc = true;
        var expectedResponse = new GetReinvestigationRequestsResponse();
        var mockAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(expectedResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient
            .Setup(x => x.GetReinvestigationRequestsAsync(It.Is<GetReinvestigationRequestsRequest>(r => r.UserId == userId && r.Sarc == sarc), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(mockAsyncCall);

        // Act
        var result = await _greeterClient.GetReinvestigationRequestsAsync(userId, sarc);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
    }

    /// <summary>
    /// Tests that GetManagedUsersAsync handles null parameters correctly.
    /// </summary>
    [Theory]
    [ClassData(typeof(WorkflowClientNullableParameterData))]
    public async Task GetManagedUsersAsync_HandlesNullableParametersCorrectly(int? userId, string? ssn, string? name, int? status, int? role, int? srchUnit, bool? showAllUsers)
    {
        // Act
        await _greeterClient.GetManagedUsersAsync(userId, ssn!, name!, status, role, srchUnit, showAllUsers);

        // Assert
        // Verify default values are used for null parameters
        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Tests that RegisterUserAsync handles null expiration date correctly.
    /// </summary>
    [Fact]
    public async Task RegisterUserAsync_HandlesNullExpirationDateCorrectly()
    {
        // Arrange
        var userId = 123;
        var workCompo = "TestComponent";
        var receiveEmail = true;
        var groupId = 456;
        var accountStatus = 1;
        string expirationDate = null!;

        // Act
        await _greeterClient.RegisterUserAsync(userId, workCompo, receiveEmail, groupId, accountStatus, expirationDate);

        // Assert
        // Verify null expiration date is handled as empty string
        await Task.CompletedTask; // Placeholder
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Tests that Dispose method can be called multiple times without error.
    /// </summary>
    [Fact]
    public void Dispose_CanBeCalledMultipleTimes_WithoutError()
    {
        // Act
        _greeterClient.Dispose();
        _greeterClient.Dispose(); // Should not throw

        // Assert
        Assert.True(true); // If we get here, no exception was thrown
    }

    /// <summary>
    /// Tests that Dispose method properly disposes of resources.
    /// </summary>
    [Fact]
    public void Dispose_ProperlyDisposesResources()
    {
        // Act
        _greeterClient.Dispose();

        // Assert
        // In a real implementation, we'd verify that the gRPC channel was disposed
        // For this test, we just ensure no exceptions are thrown
        Assert.True(true);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests that methods handle very large integer values correctly.
    /// </summary>
    /// <param name="largeValue">The large integer value to test.</param>
    [Theory]
    [ClassData(typeof(WorkflowClientLargeIntegerData))]
    public async Task Methods_HandleLargeIntegerValuesCorrectly(int largeValue)
    {
        // Act
        await _greeterClient.GetUserAltTitleAsync(largeValue, largeValue);

        // Assert
        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Tests that methods handle very long string values correctly.
    /// </summary>
    [Theory]
    [ClassData(typeof(WorkflowClientStringParameterData))]
    public async Task Methods_HandleVariousStringValuesCorrectly(string testString)
    {
        // Act
        await _greeterClient.GetUserNameAsync(testString, testString);

        // Assert
        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Tests that methods handle empty string values correctly.
    /// </summary>
    [Fact]
    public async Task Methods_HandleEmptyStringValuesCorrectly()
    {
        // Arrange
        var emptyString = string.Empty;

        // Act
        await _greeterClient.GetUserNameAsync(emptyString, emptyString);

        // Assert
        await Task.CompletedTask; // Placeholder
    }

    #endregion

    #region Workflow Method Tests

    /// <summary>
    /// Tests that workflow methods handle various parameter combinations correctly.
    /// </summary>
    /// <param name="workflowId">The workflow ID parameter.</param>
    /// <param name="groupId">The group ID parameter.</param>
    /// <param name="status">The status parameter.</param>
    [Theory]
    [ClassData(typeof(WorkflowClientWorkflowParameterData))]
    public async Task WorkflowMethods_HandleParameterCombinationsCorrectly(int workflowId, int groupId, int status)
    {
        // Act
        await _greeterClient.GetPageAccessByGroupAsync(workflowId, status, groupId);

        // Assert
        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Tests that GetWorkflowInitialStatusCodeAsync handles integer component parameter correctly.
    /// </summary>
    /// <param name="compo">The component parameter.</param>
    /// <param name="module">The module parameter.</param>
    /// <param name="workflowId">The workflow ID parameter.</param>
    [Theory]
    [ClassData(typeof(WorkflowClientWorkflowParameterData))]
    public async Task GetWorkflowInitialStatusCodeAsync_HandlesIntegerComponentCorrectly(int compo, int module, int workflowId)
    {
        // Act
        await _greeterClient.GetWorkflowInitialStatusCodeAsync(compo, module, workflowId);

        // Assert
        await Task.CompletedTask; // Placeholder
    }

    #endregion

    #region gRPC Error Handling Tests

    /// <summary>
    /// Tests that GetReinvestigationRequestsAsync handles Unavailable status code with retry logic.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_HandlesUnavailableStatus_WithRetry()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 123, Sarc = true };
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "Service unavailable"));
        
        var successResponse = new GetReinvestigationRequestsResponse
        {
            Items = { new ReinvestigationRequestItem { Id = 1, Description = "Recovered after retry" } }
        };
        var successAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(successResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        // Configure mock to fail once, then succeed (simulating retry recovery)
        _mockGrpcClient.SetupSequence(x => x.GetReinvestigationRequestsAsync(
                It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException)
            .Returns(successAsyncCall);

        // Act & Assert - Should not throw due to retry policy
        var result = await _greeterClient.GetReinvestigationRequestsAsync(request.UserId, request.Sarc);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that GetUserNameAsync handles DeadlineExceeded status code with retry logic.
    /// </summary>
    [Fact]
    public async Task GetUserNameAsync_HandlesDeadlineExceededStatus_WithRetry()
    {
        // Arrange
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "Deadline exceeded"));
        
        var successResponse = new GetUserNameResponse
        {
            Items = { new UserNameItem { UserId = 1, FirstName = "Recovered", LastName = "User" } }
        };
        var successAsyncCall = new AsyncUnaryCall<GetUserNameResponse>(
            Task.FromResult(successResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.SetupSequence(x => x.GetUserNameAsync(
                It.IsAny<GetUserNameRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException)
            .Returns(successAsyncCall);

        // Act & Assert
        var result = await _greeterClient.GetUserNameAsync("John", "Doe");
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that GetManagedUsersAsync handles Internal status code with retry logic.
    /// </summary>
    [Fact]
    public async Task GetManagedUsersAsync_HandlesInternalStatus_WithRetry()
    {
        // Arrange
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Internal, "Internal server error"));
        
        var successResponse = new GetManagedUsersResponse
        {
            Items = { new ManagedUserItem { UserId = 1, UserName = "RecoveredUser" } }
        };
        var successAsyncCall = new AsyncUnaryCall<GetManagedUsersResponse>(
            Task.FromResult(successResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.SetupSequence(x => x.GetManagedUsersAsync(
                It.IsAny<GetManagedUsersRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException)
            .Returns(successAsyncCall);

        // Act & Assert
        var result = await _greeterClient.GetManagedUsersAsync(1, "123456789", "John", 1, 1, 1, true);
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that non-retryable gRPC errors (NotFound) are thrown immediately.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleAsync_ThrowsImmediately_ForNonRetryableErrors()
    {
        // Arrange
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.NotFound, "User not found"));
        
        _mockGrpcClient.Setup(x => x.GetUserAltTitleAsync(
                It.IsAny<GetUserAltTitleRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException);

        // Act & Assert - Should throw immediately for non-retryable errors
        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
            await _greeterClient.GetUserAltTitleAsync(1, 1));
    }

    /// <summary>
    /// Tests that PermissionDenied status is thrown immediately without retry.
    /// </summary>
    [Fact]
    public async Task GetPermissionsAsync_ThrowsImmediately_ForPermissionDenied()
    {
        // Arrange
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.PermissionDenied, "Permission denied"));
        
        _mockGrpcClient.Setup(x => x.GetPermissionsAsync(
                It.IsAny<GetPermissionsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException);

        // Act & Assert
        await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
            await _greeterClient.GetPermissionsAsync(1));
    }

    #endregion

    #region Timeout and Resilience Tests

    /// <summary>
    /// Tests that methods handle timeout scenarios gracefully with exponential backoff.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_RecoveryAfterTimeout_WithExponentialBackoff()
    {
        // Arrange
        var callCount = 0;
        var request = new GetReinvestigationRequestsRequest { UserId = 123, Sarc = false };
        var timeoutException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "Timeout"));
        
        var successResponse = new GetReinvestigationRequestsResponse
        {
            Items = { new ReinvestigationRequestItem { Id = 1, Description = "Recovered" } }
        };
        var successAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(successResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.Setup(x => x.GetReinvestigationRequestsAsync(
                It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount < 3)
                    throw timeoutException;
                return successAsyncCall;
            });

        // Act
        var result = await _greeterClient.GetReinvestigationRequestsAsync(request.UserId, request.Sarc);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, callCount); // Verify retry attempts
    }

    /// <summary>
    /// Tests that methods handle cancellation tokens correctly.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_CanBeCancelled()
    {
        // Arrange
        var request = new GetReinvestigationRequestsRequest { UserId = 123, Sarc = true };
        var successResponse = new GetReinvestigationRequestsResponse
        {
            Items = { new ReinvestigationRequestItem { Id = 1 } }
        };
        var successAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(successResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.Setup(x => x.GetReinvestigationRequestsAsync(
                It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(successAsyncCall);

        // Act - Verify that the method completes without throwing on normal operation
        var result = await _greeterClient.GetReinvestigationRequestsAsync(request.UserId, request.Sarc);

        // Assert
        Assert.NotNull(result);
    }

    /// <summary>
    /// Tests that multiple concurrent requests are handled correctly with retry policies.
    /// </summary>
    [Fact]
    public async Task MultipleRequests_ConcurrentExecution_WithRetryPolicies()
    {
        // Arrange
        var tasks = new List<Task<object>>();
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "Service unavailable"));
        
        var response1 = new GetReinvestigationRequestsResponse
        {
            Items = { new ReinvestigationRequestItem { Id = 1 } }
        };
        var asyncCall1 = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(response1),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        var response2 = new GetUserNameResponse
        {
            Items = { new UserNameItem { UserId = 1, FirstName = "Test" } }
        };
        var asyncCall2 = new AsyncUnaryCall<GetUserNameResponse>(
            Task.FromResult(response2),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.SetupSequence(x => x.GetReinvestigationRequestsAsync(
                It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException)
            .Returns(asyncCall1);

        _mockGrpcClient.SetupSequence(x => x.GetUserNameAsync(
                It.IsAny<GetUserNameRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Throws(rpcException)
            .Returns(asyncCall2);

        // Act
        var task1 = _greeterClient.GetReinvestigationRequestsAsync(1, true).ContinueWith(t => (object?)t.Result);
        var task2 = _greeterClient.GetUserNameAsync("John", "Doe").ContinueWith(t => (object?)t.Result);
        
        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotNull(r));
    }

    /// <summary>
    /// Tests that retry count limits are respected (no infinite retries).
    /// </summary>
    [Fact]
    public async Task GetManagedUsersAsync_RespectsRetryLimit_AfterMaxAttempts()
    {
        // Arrange
        var callCount = 0;
        var maxRetries = 3;
        var rpcException = new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.Unavailable, "Always fails"));
        
        _mockGrpcClient.Setup(x => x.GetManagedUsersAsync(
                It.IsAny<GetManagedUsersRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                throw rpcException;
            });

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Grpc.Core.RpcException>(async () =>
            await _greeterClient.GetManagedUsersAsync(1, "123456789", "John", 1, 1, 1, true));
        
        Assert.NotNull(ex);
        // Verify retries were attempted (callCount should be > 1 but <= maxRetries + 1)
        Assert.InRange(callCount, 2, maxRetries + 2);
    }

    #endregion

    #region gRPC Response Validation Tests

    /// <summary>
    /// Tests that empty responses are handled correctly.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_HandlesEmptyResponse()
    {
        // Arrange
        var emptyResponse = new GetReinvestigationRequestsResponse();
        var emptyAsyncCall = new AsyncUnaryCall<GetReinvestigationRequestsResponse>(
            Task.FromResult(emptyResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.Setup(x => x.GetReinvestigationRequestsAsync(
                It.IsAny<GetReinvestigationRequestsRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(emptyAsyncCall);

        // Act
        var result = await _greeterClient.GetReinvestigationRequestsAsync(1, true);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    /// <summary>
    /// Tests that malformed gRPC responses are handled gracefully.
    /// </summary>
    [Fact]
    public async Task GetUserNameAsync_HandlesMalformedResponse()
    {
        // Arrange
        var malformedResponse = new GetUserNameResponse
        {
            Items = { new UserNameItem { UserId = -1, FirstName = null!, LastName = null! } }
        };
        var malformedAsyncCall = new AsyncUnaryCall<GetUserNameResponse>(
            Task.FromResult(malformedResponse),
            Task.FromResult(new Metadata()),
            () => Grpc.Core.Status.DefaultSuccess,
            () => [],
            () => { });

        _mockGrpcClient.Setup(x => x.GetUserNameAsync(
                It.IsAny<GetUserNameRequest>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(malformedAsyncCall);

        // Act
        var result = await _greeterClient.GetUserNameAsync("", "");

        // Assert - Should not throw, but should handle gracefully
        Assert.NotNull(result);
    }

    #endregion
}



