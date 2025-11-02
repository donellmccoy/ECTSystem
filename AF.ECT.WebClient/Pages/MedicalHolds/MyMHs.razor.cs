using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.MH;

/// <summary>
/// Code-behind for the MyMHs page.
/// </summary>
public partial class MyMHs
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
