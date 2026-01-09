using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.Cases.Administrative.PEPP;

/// <summary>
/// Code-behind for the MyPEPPs page.
/// </summary>
public partial class MyPEPPs
{
    /// <summary>
    /// Gets or sets the workflow client for gRPC communication.
    /// </summary>
    [Inject]
    private IWorkflowClient WorkflowClient { get; set; } = default!;
    /// <summary>
    /// Initializes the component.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
}
