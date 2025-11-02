using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.LOD;

/// <summary>
/// Code-behind for the APSearch page.
/// </summary>
public partial class APSearch
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
