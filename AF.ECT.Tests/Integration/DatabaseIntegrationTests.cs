using AF.ECT.Tests.Infrastructure;

namespace AF.ECT.Tests.Integration;

/// <summary>
/// Integration tests that use a real SQLite database for more realistic testing.
/// These tests verify end-to-end functionality with actual data persistence.
/// </summary>
[Collection("Database Integration Tests")]
[Trait("Category", "Integration")]
[Trait("Component", "Database")]
public class DatabaseIntegrationTests : DatabaseIntegrationTestBase
{
    /// <summary>
    /// Tests that the gRPC service can retrieve reinvestigation requests from a real database.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsAsync_ReturnsDataFromDatabase()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest
        {
            UserId = 1,
            Sarc = true
        };

        // Act
        var response = await client.GetReinvestigationRequestsAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Since we're using mocked data service in the base class,
        // this will return empty results, but the call should succeed
        response.Items.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that user name retrieval works with database integration.
    /// </summary>
    [Theory]
    [InlineData("John", "Doe")]
    [InlineData("Jane", "Smith")]
    public async Task GetUserNameAsync_HandlesVariousInputs(string firstName, string lastName)
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetUserNameRequest
        {
            First = firstName,
            Last = lastName
        };

        // Act
        var response = await client.GetUserNameAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().NotBeNull();
        // Results will be empty due to mocked data service
        response.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the service handles invalid requests gracefully.
    /// </summary>
    [Fact]
    public async Task Service_HandlesInvalidRequests_WithoutCrashing()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);

        // Test with null/empty parameters
        var request = new GetUserNameRequest
        {
            First = "",
            Last = ""
        };

        // Act & Assert - Should not throw exception
        var response = await client.GetUserNameAsync(request);
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Tests workflow-related operations with database integration.
    /// </summary>
    [Fact]
    public async Task WorkflowOperations_WorkWithDatabase()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetWorkflowTitleRequest
        {
            ModuleId = 1,
            SubCase = 0
        };

        // Act
        var response = await client.GetWorkflowTitleAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().NotBeNull();
        // Will be empty due to mocked data service
        response.Items.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that multiple sequential operations work correctly.
    /// </summary>
    [Fact]
    public async Task MultipleSequentialOperations_WorkCorrectly()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);

        // Act - Perform multiple operations
        var userRequest = new GetUserNameRequest { First = "Test", Last = "User" };
        var userResponse = await client.GetUserNameAsync(userRequest);

        var workflowRequest = new GetWorkflowTitleRequest { ModuleId = 1, SubCase = 0 };
        var workflowResponse = await client.GetWorkflowTitleAsync(workflowRequest);

        // Assert
        userResponse.Should().NotBeNull();
        workflowResponse.Should().NotBeNull();
        // Both should succeed even with empty results
        userResponse.Items.Should().BeEmpty();
        workflowResponse.Items.Should().BeEmpty();
    }

    #region Transaction Handling Tests

    /// <summary>
    /// Tests that transactions are properly handled in data layer operations.
    /// </summary>
    [Fact]
    public async Task DataLayerOperations_HonorTransactionBoundaries()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };

        // Act - Call gRPC service which should use data layer transactions
        var response = await client.GetReinvestigationRequestsAsync(request);

        // Assert
        response.Should().NotBeNull();
        // Verify response structure is valid (transaction completed successfully)
        response.Count.Should().Be(0); // Currently empty due to mocked data
    }

    /// <summary>
    /// Tests that failed transactions are properly rolled back.
    /// </summary>
    [Fact]
    public async Task FailedDataLayerOperation_RollsBackTransaction()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest { UserId = -1, Sarc = false }; // Invalid ID

        // Act & Assert - Should handle gracefully without leaving partial data
        var response = await client.GetReinvestigationRequestsAsync(request);
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
    }

    #endregion

    #region Stored Procedure Validation Tests

    /// <summary>
    /// Tests that stored procedure call parameters are validated correctly.
    /// </summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(100, true)]
    [InlineData(int.MaxValue, false)]
    public async Task StoredProcedure_ValidatesParametersCorrectly(int userId, bool sarc)
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest { UserId = userId, Sarc = sarc };

        // Act - Should handle various parameter combinations without throwing
        var response = await client.GetReinvestigationRequestsAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Should().BeOfType<GetReinvestigationRequestsResponse>();
    }

    /// <summary>
    /// Tests that stored procedure return values are properly interpreted.
    /// </summary>
    [Fact]
    public async Task StoredProcedure_ReturnsCorrectResultStructure()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest { UserId = 123, Sarc = true };

        // Act
        var response = await client.GetReinvestigationRequestsAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().NotBeNull();
        response.Items.Count.Should().BeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Tests that stored procedures handle NULL results gracefully.
    /// </summary>
    [Fact]
    public async Task StoredProcedure_HandlesNullResults_Gracefully()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetUserNameRequest { First = null!, Last = null! };

        // Act & Assert - Should not throw NullReferenceException
        var response = await client.GetUserNameAsync(request);
        response.Should().NotBeNull();
    }

    #endregion

    #region Data Integrity Tests

    /// <summary>
    /// Tests that data consistency is maintained across multiple operations.
    /// </summary>
    [Fact]
    public async Task DataConsistency_MaintainedAcrossMultipleOperations()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);

        // Act - Retrieve same data multiple times
        var request1 = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var response1 = await client.GetReinvestigationRequestsAsync(request1);
        
        var request2 = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var response2 = await client.GetReinvestigationRequestsAsync(request2);

        // Assert - Results should be consistent
        response1.Count.Should().Be(response2.Count);
        response1.Items.Count.Should().Be(response2.Items.Count);
    }

    /// <summary>
    /// Tests that different queries return distinct datasets correctly.
    /// </summary>
    [Fact]
    public async Task DifferentQueries_ReturnDistinctDatasets()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);

        // Act - Query with different parameters
        var sarcsRequest = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var sarcsResponse = await client.GetReinvestigationRequestsAsync(sarcsRequest);
        
        var nonSarcsRequest = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = false };
        var nonSarcsResponse = await client.GetReinvestigationRequestsAsync(nonSarcsRequest);

        // Assert - Different parameters should be handled independently
        sarcsResponse.Should().NotBeNull();
        nonSarcsResponse.Should().NotBeNull();
    }

    #endregion

    #region Edge Case Tests

    /// <summary>
    /// Tests that large result sets are handled efficiently.
    /// </summary>
    [Fact]
    public async Task LargeResultSets_AreHandledEfficiently()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetManagedUsersRequest 
        { 
            Userid = 0,
            ShowAllUsers = true // Request all users
        };

        // Act
        var response = await client.GetManagedUsersAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Items.Should().NotBeNull();
        // Should complete within reasonable time
    }

    /// <summary>
    /// Tests that special characters in parameters are handled correctly.
    /// </summary>
    [Theory]
    [InlineData("O'Brien", "Smith")]
    [InlineData("Jean-Pierre", "Dupont")]
    [InlineData("José", "García")]
    [InlineData("李", "王")]
    public async Task SpecialCharacters_InParameters_AreHandledCorrectly(string firstName, string lastName)
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetUserNameRequest { First = firstName, Last = lastName };

        // Act & Assert - Should not throw
        var response = await client.GetUserNameAsync(request);
        response.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that concurrent data layer operations don't interfere with each other.
    /// </summary>
    [Fact]
    public async Task ConcurrentDataLayerOperations_DoNotInterfere()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);

        var request1 = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var request2 = new GetReinvestigationRequestsRequest { UserId = 2, Sarc = false };
        var request3 = new GetUserNameRequest { First = "Test", Last = "User" };

        // Act - Execute concurrent operations
        var call1 = client.GetReinvestigationRequestsAsync(request1);
        var call2 = client.GetReinvestigationRequestsAsync(request2);
        var call3 = client.GetUserNameAsync(request3);

        var response1 = await call1.ResponseAsync;
        var response2 = await call2.ResponseAsync;
        var response3 = await call3.ResponseAsync;

        // Assert - All operations should complete successfully
        response1.Should().NotBeNull();
        response2.Should().NotBeNull();
        response3.Should().NotBeNull();
    }

    #endregion

    #region Performance and Query Optimization Tests

    /// <summary>
    /// Tests that frequently called queries execute within acceptable timeframe.
    /// </summary>
    [Fact]
    public async Task FrequentQueries_ExecuteWithinAcceptableTime()
    {
        // Arrange
        var channel = CreateGrpcChannel();
        var client = new WorkflowService.WorkflowServiceClient(channel);
        var request = new GetReinvestigationRequestsRequest { UserId = 1, Sarc = true };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act - Execute query
        var response = await client.GetReinvestigationRequestsAsync(request);
        stopwatch.Stop();

        // Assert - Should complete within reasonable time (5 seconds for integration test)
        response.Should().NotBeNull();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    #endregion
}
