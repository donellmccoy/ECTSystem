using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.SexualAssaultResponseCoordinator;

/// <summary>
/// Code-behind for the SARCMember page.
/// </summary>
public partial class SARCMember
{
    /// <summary>
    /// Gets or sets the workflow client for gRPC communication.
    /// </summary>
    [Inject]
    private IWorkflowClient WorkflowClient { get; set; } = default!;
    /// <summary>
    /// Gets or sets the case ID route parameter.
    /// </summary>
    [Parameter]
    public int CaseId { get; set; }
    /// <summary>
    /// Initializes the component.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
}
