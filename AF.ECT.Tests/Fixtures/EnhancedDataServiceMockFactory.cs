namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Factory for creating pre-configured DataService mocks with common test scenarios.
/// Reduces boilerplate mock setup and ensures consistency across test suites.
/// </summary>
public static class EnhancedDataServiceMockFactory
{
    /// <summary>
    /// Creates a DataService mock with default empty responses for all methods.
    /// </summary>
    /// <returns>A minimally configured mock IDataService</returns>
    public static Mock<IDataService> CreateDefaultEmptyMock()
    {
        var mock = new Mock<IDataService>(MockBehavior.Strict);
        
        // Setup all common methods to return empty collections
        mock.Setup(ds => ds.GetUsersOnlineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUsersOnlineResult>());

        mock.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_lod_sp_GetReinvestigationRequestsResult>());

        mock.Setup(ds => ds.SearchMemberDataAsync(It.IsAny<SearchMemberDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_SearchMemberDataResult>());

        return mock;
    }
}
