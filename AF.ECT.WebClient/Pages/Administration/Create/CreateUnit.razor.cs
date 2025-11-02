using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.Administration.Create;

/// <summary>
/// Code-behind for the CreateUnit page.
/// </summary>
public partial class CreateUnit
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
