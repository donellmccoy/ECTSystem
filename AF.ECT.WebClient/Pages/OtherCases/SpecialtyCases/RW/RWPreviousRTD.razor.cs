using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.OtherCases.SpecialtyCases.RW;

/// <summary>
/// Code-behind for the RWPreviousRTD page.
/// </summary>
public partial class RWPreviousRTD
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
