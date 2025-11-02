using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.OtherCases.SpecialtyCases.MMSO;

/// <summary>
/// Code-behind for the MMNextAction page.
/// </summary>
public partial class MMNextAction
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
