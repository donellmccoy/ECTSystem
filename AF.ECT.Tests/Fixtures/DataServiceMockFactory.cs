namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Factory for creating configured Mock&lt;IDataService&gt; instances.
/// Consolidates repetitive mock setup logic into reusable, documented methods.
/// Reduces boilerplate across integration tests and enables consistent default behavior.
/// </summary>
public class DataServiceMockFactory
{
    /// <summary>
    /// Creates a new Mock&lt;IDataService&gt; with all methods configured to return empty results by default.
    /// This prevents NullReferenceException and provides a safe baseline for testing.
    /// </summary>
    /// <returns>A fully configured mock IDataService instance</returns>
    public static Mock<IDataService> CreateDefaultMock()
    {
        var mockDataService = new Mock<IDataService>();
        ConfigureListReturningMethods(mockDataService);
        ConfigureIntReturningMethods(mockDataService);
        ConfigureSpecialMethods(mockDataService);
        return mockDataService;
    }

    /// <summary>
    /// Creates a mock IDataService with custom return values for specific methods.
    /// </summary>
    /// <param name="configureAction">Action to configure the mock with custom behavior</param>
    /// <returns>A configured mock IDataService instance</returns>
    public static Mock<IDataService> CreateCustomMock(Action<Mock<IDataService>> configureAction)
    {
        var mockDataService = CreateDefaultMock();
        configureAction(mockDataService);
        return mockDataService;
    }

    #region Private Configuration Methods

    /// <summary>
    /// Configures all methods that return lists to return empty lists by default.
    /// </summary>
    private static void ConfigureListReturningMethods(Mock<IDataService> mock)
    {
        // User-related methods
        mock.Setup(ds => ds.GetUsersOnlineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUsersOnlineResult>());

        mock.Setup(ds => ds.SearchMemberDataAsync(It.IsAny<SearchMemberDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_SearchMemberDataResult>());

        mock.Setup(ds => ds.GetMailingListForLODAsync(It.IsAny<GetMailingListForLODRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetMailingListForLODResult>());

        mock.Setup(ds => ds.GetManagedUsersAsync(It.IsAny<GetManagedUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetManagedUsersResult>());

        mock.Setup(ds => ds.GetUserAltTitleAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUserAltTitleResult>());

        mock.Setup(ds => ds.GetUserAltTitleByGroupCompoAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUserAltTitleByGroupCompoResult>());

        mock.Setup(ds => ds.GetUserNameAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUserNameResult>());

        mock.Setup(ds => ds.GetUsersAltTitleByGroupAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetUsersAltTitleByGroupResult>());

        mock.Setup(ds => ds.GetWhoisAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_GetWhoisResult>());

        mock.Setup(ds => ds.HasHQTechAccountAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_HasHQTechAccountResult>());

        mock.Setup(ds => ds.IsFinalStatusCodeAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_IsFinalStatusCodeResult>());

        mock.Setup(ds => ds.UpdateLoginAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_UpdateLoginResult>());

        mock.Setup(ds => ds.SearchMemberDataTestAsync(It.IsAny<SearchMemberDataTestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_user_sp_SearchMemberData_TestResult>());

        // Workflow-related methods
        mock.Setup(ds => ds.GetAllFindingByReasonOfAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetAllFindingByReasonOfResult>());

        mock.Setup(ds => ds.GetAllLocksAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetAllLocksResult>());

        mock.Setup(ds => ds.GetCancelReasonsAsync(It.IsAny<byte?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetCancelReasonsResult>());

        mock.Setup(ds => ds.GetWorkflowTitleAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetWorkflowTitleResult>());

        mock.Setup(ds => ds.GetPermissionsAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_Workflow_sp_GetPermissionsResult>());

        mock.Setup(ds => ds.GetReinvestigationRequestsAsync(It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_lod_sp_GetReinvestigationRequestsResult>());

        mock.Setup(ds => ds.GetActionsByStepAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetActionsByStepResult>());

        mock.Setup(ds => ds.GetActiveCasesAsync(It.IsAny<int?>(), It.IsAny<short?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetActiveCasesResult>());

        mock.Setup(ds => ds.GetCreatableByGroupAsync(It.IsAny<string?>(), It.IsAny<byte?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetCreatableByGroupResult>());

        mock.Setup(ds => ds.GetFindingByReasonOfByIdAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetFindingByReasonOfByIdResult>());

        mock.Setup(ds => ds.GetFindingsAsync(It.IsAny<byte?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetFindingsResult>());

        mock.Setup(ds => ds.GetModuleFromWorkflowAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetModuleFromWorkflowResult>());

        mock.Setup(ds => ds.GetPageAccessByGroupAsync(It.IsAny<byte?>(), It.IsAny<int?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetPageAccessByGroupResult>());

        mock.Setup(ds => ds.GetPageAccessByWorkflowViewAsync(It.IsAny<string?>(), It.IsAny<byte?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetPageAccessByWorkflowViewResult>());

        mock.Setup(ds => ds.GetPagesByWorkflowIdAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetPagesByWorkflowIdResult>());

        mock.Setup(ds => ds.GetPermissionsByCompoAsync(It.IsAny<byte?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_Workflow_sp_GetPermissionsByCompoResult>());

        mock.Setup(ds => ds.GetReturnReasonsAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetReturnReasonsResult>());

        mock.Setup(ds => ds.GetRwoaReasonsAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetRwoaReasonsResult>());

        mock.Setup(ds => ds.GetStatusCodesByCompoAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodesByCompoResult>());

        mock.Setup(ds => ds.GetStatusCodesByCompoAndModuleAsync(It.IsAny<string?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodesByCompoAndModuleResult>());

        mock.Setup(ds => ds.GetStatusCodesBySignCodeAsync(It.IsAny<short?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodesBySignCodeResult>());

        mock.Setup(ds => ds.GetStatusCodesByWorkflowAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodesByWorkflowResult>());

        mock.Setup(ds => ds.GetStatusCodesByWorkflowAndAccessScopeAsync(It.IsAny<byte?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodesByWorkflowAndAccessScopeResult>());

        mock.Setup(ds => ds.GetStatusCodeScopeAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStatusCodeScopeResult>());

        mock.Setup(ds => ds.GetStepsByWorkflowAsync(It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStepsByWorkflowResult>());

        mock.Setup(ds => ds.GetStepsByWorkflowAndStatusAsync(It.IsAny<byte?>(), It.IsAny<byte?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetStepsByWorkflowAndStatusResult>());

        mock.Setup(ds => ds.GetViewableByGroupAsync(It.IsAny<byte?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetViewableByGroupResult>());

        mock.Setup(ds => ds.GetWorkflowByCompoAsync(It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetWorkflowByCompoResult>());

        mock.Setup(ds => ds.GetWorkflowFromModuleAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetWorkflowFromModuleResult>());

        mock.Setup(ds => ds.GetWorkflowInitialStatusCodeAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetWorkflowInitialStatusCodeResult>());

        mock.Setup(ds => ds.GetWorkflowTitleByWorkStatusIdAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_GetWorkflowTitleByWorkStatusIdResult>());

        mock.Setup(ds => ds.AddSignatureAsync(It.IsAny<AddSignatureRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_AddSignatureResult>());

        mock.Setup(ds => ds.CopyWorkflowAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_CopyWorkflowResult>());

        mock.Setup(ds => ds.InsertActionAsync(It.IsAny<InsertActionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_InsertActionResult>());

        mock.Setup(ds => ds.InsertOptionActionAsync(It.IsAny<InsertOptionActionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<core_workflow_sp_InsertOptionActionResult>());
    }

    /// <summary>
    /// Configures all methods that return integers to return 0 by default.
    /// </summary>
    private static void ConfigureIntReturningMethods(Mock<IDataService> mock)
    {
        mock.Setup(ds => ds.GetMembersUserIdAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.LogoutAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.RegisterUserAsync(It.IsAny<RegisterUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.RegisterUserRoleAsync(It.IsAny<int?>(), It.IsAny<short?>(), It.IsAny<byte?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.UpdateAccountStatusAsync(It.IsAny<UpdateAccountStatusRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.UpdateManagedSettingsAsync(It.IsAny<UpdateManagedSettingsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.UpdateUserAltTitleAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.CopyActionsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.CopyRulesAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        mock.Setup(ds => ds.DeleteStatusCodeAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    /// <summary>
    /// Configures special methods with custom behavior.
    /// </summary>
    private static void ConfigureSpecialMethods(Mock<IDataService> mock)
    {
        // Add any special method configurations here
        // Example: Methods with complex return types or specific behaviors
    }

    #endregion
}
