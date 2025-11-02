using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.Help.Documentation;

/// <summary>
/// Code-behind for the AFRCPHAGuideMar2015 page.
/// </summary>
public partial class AFRCPHAGuideMar2015
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
