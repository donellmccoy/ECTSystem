using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.Help.Documentation;

/// <summary>
/// Code-behind for the DODI1332_38 page.
/// </summary>
public partial class DODI1332_38
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
