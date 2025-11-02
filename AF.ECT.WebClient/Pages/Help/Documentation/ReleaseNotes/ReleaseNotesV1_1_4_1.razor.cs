using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.Help.Documentation.ReleaseNotes;

/// <summary>
/// Code-behind for the ReleaseNotesV1_1_4_1 page.
/// </summary>
public partial class ReleaseNotesV1_1_4_1
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
