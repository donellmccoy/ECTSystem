# Part 2: Reports, Administration, Help, and Workflow Pages Generation Script
# This continues from Generate-RazorPages.ps1

$webClientPath = "d:\source\repos\dmccoy2025\ECTSystem\AF.ECT.WebClient\Pages"

# Function to create a Razor page and code-behind file
function New-RazorPage {
    param(
        [string]$FolderPath,
        [string]$FileName,
        [string]$Route,
        [string[]]$Roles,
        [string]$PageTitle,
        [bool]$HasRouteParam = $false
    )

    $razorFile = Join-Path $FolderPath "$FileName.razor"
    $codeFile = Join-Path $FolderPath "$FileName.razor.cs"

    # Ensure directory exists
    if (-not (Test-Path $FolderPath)) {
        New-Item -ItemType Directory -Path $FolderPath -Force | Out-Null
    }

    # Determine authorization attribute
    $authAttribute = ""
    if ($Roles -and $Roles.Count -gt 0 -and $Roles[0] -ne "*") {
        $rolesStr = ($Roles | ForEach-Object { "UserRole.$_" }) -join ", "
        $authAttribute = "@attribute [Authorize(Roles = `"$($Roles -join ',')`")]"
    }
    else {
        $authAttribute = "@attribute [Authorize]"
    }

    # Determine route parameter
    $routeParam = if ($HasRouteParam) { "/{caseId:int}" } else { "" }

    # Create Razor file content
    $razorContent = @"
@page "$Route$routeParam"
$authAttribute
@using Microsoft.AspNetCore.Authorization

<PageTitle>$PageTitle</PageTitle>

<h1>$PageTitle</h1>

<p>This page is under development.</p>
"@

    # Extract namespace from folder path
    $relativePath = $FolderPath.Replace($webClientPath, "").TrimStart('\').Replace('\', '.')
    $namespace = if ($relativePath) { "AF.ECT.WebClient.Pages.$relativePath" } else { "AF.ECT.WebClient.Pages" }

    # Create code-behind file content
    $codeContent = @"
using Microsoft.AspNetCore.Components;

namespace $namespace;

/// <summary>
/// Code-behind for the $FileName page.
/// </summary>
public partial class $FileName
{
    /// <summary>
    /// Gets or sets the workflow client for gRPC communication.
    /// </summary>
    [Inject]
    private IWorkflowClient WorkflowClient { get; set; } = default!;
"@

    if ($HasRouteParam) {
        $codeContent += @"

    /// <summary>
    /// Gets or sets the case ID route parameter.
    /// </summary>
    [Parameter]
    public int CaseId { get; set; }
"@
    }

    $codeContent += @"

    /// <summary>
    /// Initializes the component.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }
}
"@

    # Write files
    Set-Content -Path $razorFile -Value $razorContent -Encoding UTF8
    Set-Content -Path $codeFile -Value $codeContent -Encoding UTF8

    Write-Host "Created: $FileName" -ForegroundColor Green
}

# Reports Pages
Write-Host "`nCreating Reports pages..." -ForegroundColor Cyan
$reportsPath = Join-Path $webClientPath "Reports"
$reportPages = @(
    @{Name="AdHocReporting"; Route="/reports/ad-hoc-reporting"; Title="Ad-Hoc Reporting"; Roles=@("AdHocReport","SARCAdHocReports")},
    @{Name="AdHocReportingResults"; Route="/reports/ad-hoc-reporting-results"; Title="Ad-Hoc Reporting Results"; Roles=@("AdHocReport","SARCAdHocReports")},
    @{Name="CaseHistoryReport"; Route="/reports/case-history-report"; Title="Case History Report"; Roles=@("LodViewAllCases")},
    @{Name="LODCategoryCountReport"; Route="/reports/lod-category-count-report"; Title="LOD Category Count Report"; Roles=@("LODCategoryCountReport")},
    @{Name="LODDisapprovedReport"; Route="/reports/lod-disapproved-report"; Title="LOD Disapproved Report"; Roles=@("DisapprovedLODReport")},
    @{Name="LODDispositionReport"; Route="/reports/lod-disposition-report"; Title="LOD Disposition Report"; Roles=@("DispositionReports")},
    @{Name="LODGraphReport"; Route="/reports/lod-graph-report"; Title="LOD Graph Report"; Roles=@("LODGraphReport")},
    @{Name="LODMetricsReportLODCases"; Route="/reports/lod-metrics-report-lod-cases"; Title="LOD Metrics Report (LOD Cases)"; Roles=@("LODMetricsReport_Cases")},
    @{Name="LODMetricsReportAverageTimes"; Route="/reports/lod-metrics-report-average-times"; Title="LOD Metrics Report (Average Times)"; Roles=@("LODMetricsReport_Avg")},
    @{Name="LODPackagesExecutionReport"; Route="/reports/lod-packages-execution-report"; Title="LOD Packages Execution Report"; Roles=@("SysAdmin")},
    @{Name="LODPhysicianCancelledReport"; Route="/reports/lod-physician-cancelled-report"; Title="LOD Physician Cancelled Report"; Roles=@("LODPhysicianCancelledReport")},
    @{Name="LODPMComplianceReport"; Route="/reports/lod-pm-compliance-report"; Title="LOD PM Compliance Report"; Roles=@("LODComplianceReport")},
    @{Name="LODPMMonthlySuspenseMonitoringReport"; Route="/reports/lod-pm-monthly-suspense-monitoring-report"; Title="LOD PM Monthly Suspense Monitoring Report"; Roles=@("LODSuspenseMonitoringReport")},
    @{Name="LODPMQuarterlyAnnualProgramStatusReport"; Route="/reports/lod-pm-quarterly-annual-program-status-report"; Title="LOD PM Quarterly-Annual Program Status Report"; Roles=@("LODProgramStatusReport")},
    @{Name="LODRWOAReport"; Route="/reports/lod-rwoa-report"; Title="LOD RWOA Report"; Roles=@("RWOAReport")},
    @{Name="LODSARCReport"; Route="/reports/lod-sarc-report"; Title="LOD SARC Report"; Roles=@("LODSARCCasesReportUnrestricted","LODSARCCasesReportAll")},
    @{Name="LODStatisticsReport"; Route="/reports/lod-statistics-report"; Title="LOD Statistics Report"; Roles=@("LODStatisticsReport")},
    @{Name="LODTotalCountByProcessReport"; Route="/reports/lod-total-count-by-process-report"; Title="LOD Total Count By Process Report"; Roles=@("LODTotalCountByProcessReport")},
    @{Name="PALDocuments"; Route="/reports/pal-documents"; Title="PAL Documents"; Roles=@("PALDocumentsReport")},
    @{Name="PHTotalsReport"; Route="/reports/ph-totals-report"; Title="PH Totals Report"; Roles=@("PHTotalsReport")},
    @{Name="PWaiversReport"; Route="/reports/pwaivers-report"; Title="PWaivers Report"; Roles=@("PwaiverReport")},
    @{Name="RSDispositionReport"; Route="/reports/rs-disposition-report"; Title="RS Disposition Report"; Roles=@("RSDispositionReport")},
    @{Name="RFACasesByUnitReport"; Route="/reports/rfa-cases-by-unit-report"; Title="RFA Cases By Unit Report"; Roles=@("RFAReportByUnit")},
    @{Name="RFACasesByGroupReport"; Route="/reports/rfa-cases-by-group-report"; Title="RFA Cases By Group Report"; Roles=@("RFAReportByGroup")}
)

foreach ($page in $reportPages) {
    New-RazorPage -FolderPath $reportsPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration Pages
Write-Host "`nCreating Administration pages..." -ForegroundColor Cyan

# Administration/Create
$adminCreatePath = Join-Path $webClientPath "Administration\Create"
$adminCreatePages = @(
    @{Name="AddMember"; Route="/administration/create/add-member"; Title="Add Member"; Roles=@("SysAdmin","MemberAdd")},
    @{Name="CreateUnit"; Route="/administration/create/create-unit"; Title="Create Unit"; Roles=@("SysAdmin")},
    @{Name="CreateUser"; Route="/administration/create/create-user"; Title="Create User"; Roles=@("SysAdmin")}
)
foreach ($page in $adminCreatePages) {
    New-RazorPage -FolderPath $adminCreatePath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Emails
$adminEmailsPath = Join-Path $webClientPath "Administration\Emails"
$adminEmailPages = @(
    @{Name="EmailTemplates"; Route="/administration/emails/email-templates"; Title="Email Templates"; Roles=@("SysAdmin")},
    @{Name="EmailTest"; Route="/administration/emails/email-test"; Title="Email Test"; Roles=@("SysAdmin")},
    @{Name="InactiveEmailSettings"; Route="/administration/emails/inactive-email-settings"; Title="Inactive Email Settings"; Roles=@("SysAdmin")},
    @{Name="SendSystemEmails"; Route="/administration/emails/send-system-emails"; Title="Send System Emails"; Roles=@("SysAdmin")}
)
foreach ($page in $adminEmailPages) {
    New-RazorPage -FolderPath $adminEmailsPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Lookup
$adminLookupPath = Join-Path $webClientPath "Administration\Lookup"
$adminLookupPages = @(
    @{Name="ARCNetUserLookup"; Route="/administration/lookup/arcnet-user-lookup"; Title="ARCNet User Lookup"; Roles=@("SysAdmin")},
    @{Name="ApprovalAuthorities"; Route="/administration/lookup/approval-authorities"; Title="Approval Authorities"; Roles=@("SysAdmin")},
    @{Name="CaseLocks"; Route="/administration/lookup/case-locks"; Title="Case Locks"; Roles=@("SysAdmin")},
    @{Name="ChildUnitView"; Route="/administration/lookup/child-unit-view"; Title="Child Unit View"; Roles=@("SysAdmin")},
    @{Name="PendingRoleRequests"; Route="/administration/lookup/pending-role-requests"; Title="Pending Role Requests"; Roles=@("UsersEdit","SysAdmin","UsersApprove")},
    @{Name="PermissionReport"; Route="/administration/lookup/permission-report"; Title="Permission Report"; Roles=@("SysAdmin")},
    @{Name="SearchMembers"; Route="/administration/lookup/search-members"; Title="Search Members"; Roles=@("UsersEdit","UsersApprove","SysAdmin")},
    @{Name="UsersOnline"; Route="/administration/lookup/users-online"; Title="Users Online"; Roles=@("SysAdmin")}
)
foreach ($page in $adminLookupPages) {
    New-RazorPage -FolderPath $adminLookupPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Manage/Groups
$adminManageGroupsPath = Join-Path $webClientPath "Administration\Manage\Groups"
$adminGroupPages = @(
    @{Name="EditGroups"; Route="/administration/manage/groups/edit-groups"; Title="Edit Groups"; Roles=@("SysAdmin")},
    @{Name="GroupApproval"; Route="/administration/manage/groups/group-approval"; Title="Group Approval"; Roles=@("SysAdmin")},
    @{Name="GroupPermissions"; Route="/administration/manage/groups/group-permissions"; Title="Group Permissions"; Roles=@("SysAdmin")}
)
foreach ($page in $adminGroupPages) {
    New-RazorPage -FolderPath $adminManageGroupsPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Manage
$adminManagePath = Join-Path $webClientPath "Administration\Manage"
$adminManagePages = @(
    @{Name="ManageCancelReasons"; Route="/administration/manage/manage-cancel-reasons"; Title="Manage Cancel Reasons"; Roles=@("SysAdmin")},
    @{Name="ManageLookups"; Route="/administration/manage/manage-lookups"; Title="Manage Lookups"; Roles=@("SysAdmin")},
    @{Name="EditCaseTypes"; Route="/administration/manage/edit-case-types"; Title="Edit Case Types"; Roles=@("SysAdmin")},
    @{Name="EditCertificationStamps"; Route="/administration/manage/edit-certification-stamps"; Title="Edit Certification Stamps"; Roles=@("SysAdmin")},
    @{Name="EditCompletedByGroups"; Route="/administration/manage/edit-completed-by-groups"; Title="Edit Completed By Groups"; Roles=@("SysAdmin")},
    @{Name="EditDispositions"; Route="/administration/manage/edit-dispositions"; Title="Edit Dispositions"; Roles=@("SysAdmin")},
    @{Name="EditMiscellaneousLookups"; Route="/administration/manage/edit-miscellaneous-lookups"; Title="Edit Miscellaneous Lookups"; Roles=@("SysAdmin")},
    @{Name="EditLODMedical"; Route="/administration/manage/edit-lod-medical"; Title="Edit LOD Medical"; Roles=@("SysAdmin")},
    @{Name="EditLODUnit"; Route="/administration/manage/edit-lod-unit"; Title="Edit LOD Unit"; Roles=@("SysAdmin")},
    @{Name="ManageHyperLinks"; Route="/administration/manage/manage-hyper-links"; Title="Manage Hyper Links"; Roles=@("SysAdmin")},
    @{Name="ManageMessages"; Route="/administration/manage/manage-messages"; Title="Manage Messages"; Roles=@("MsgAdmin")},
    @{Name="ManageUnits"; Route="/administration/manage/manage-units"; Title="Manage Units"; Roles=@("UnitsEdit")},
    @{Name="ManageUsers"; Route="/administration/manage/manage-users"; Title="Manage Users"; Roles=@("UsersEdit","UsersApprove","SysAdmin")},
    @{Name="MappedValues"; Route="/administration/manage/mapped-values"; Title="Mapped Values"; Roles=@("SysAdmin")}
)
foreach ($page in $adminManagePages) {
    New-RazorPage -FolderPath $adminManagePath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Manage/ManagePHWorkflowForm
$adminPHWorkflowPath = Join-Path $webClientPath "Administration\Manage\ManagePHWorkflowForm"
$adminPHPages = @(
    @{Name="EditPHFields"; Route="/administration/manage/manage-ph-workflow-form/edit-ph-fields"; Title="Edit PH Fields"; Roles=@("SysAdmin")},
    @{Name="EditPHFieldTypes"; Route="/administration/manage/manage-ph-workflow-form/edit-ph-field-types"; Title="Edit PH Field Types"; Roles=@("SysAdmin")},
    @{Name="EditPHFormFields"; Route="/administration/manage/manage-ph-workflow-form/edit-ph-form-fields"; Title="Edit PH Form Fields"; Roles=@("SysAdmin")},
    @{Name="EditPHSections"; Route="/administration/manage/manage-ph-workflow-form/edit-ph-sections"; Title="Edit PH Sections"; Roles=@("SysAdmin")},
    @{Name="EditPHWorkflowForm"; Route="/administration/manage/manage-ph-workflow-form/edit-ph-workflow-form"; Title="Edit PH Workflow Form"; Roles=@("SysAdmin")},
    @{Name="TestPHForm"; Route="/administration/manage/manage-ph-workflow-form/test-ph-form"; Title="Test PH Form"; Roles=@("SysAdmin")}
)
foreach ($page in $adminPHPages) {
    New-RazorPage -FolderPath $adminPHWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Manage/Pages
$adminManagePagesPath = Join-Path $webClientPath "Administration\Manage\Pages"
$adminPagePages = @(
    @{Name="ManagePages"; Route="/administration/manage/pages/manage-pages"; Title="Manage Pages"; Roles=@("SysAdmin")},
    @{Name="PageAccess"; Route="/administration/manage/pages/page-access"; Title="Page Access"; Roles=@("SysAdmin")},
    @{Name="WelcomePageBanner"; Route="/administration/manage/pages/welcome-page-banner"; Title="Welcome Page Banner"; Roles=@("SysAdmin")}
)
foreach ($page in $adminPagePages) {
    New-RazorPage -FolderPath $adminManagePagesPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/Manage/Workflows
$adminWorkflowsPath = Join-Path $webClientPath "Administration\Manage\Workflows"
New-RazorPage -FolderPath $adminWorkflowsPath -FileName "WorkflowView" -Route "/administration/manage/workflows/workflow-view" -Roles @("SysAdmin") -PageTitle "Workflow View"

# Administration/Manage/Memo
$adminMemoPath = Join-Path $webClientPath "Administration\Manage\Memo"
$adminMemoPages = @(
    @{Name="MemoMappedValues"; Route="/administration/manage/memo/memo-mapped-values"; Title="Memo Mapped Values"; Roles=@("SysAdmin")},
    @{Name="MemoTemplates"; Route="/administration/manage/memo/memo-templates"; Title="Memo Templates"; Roles=@("SysAdmin")}
)
foreach ($page in $adminMemoPages) {
    New-RazorPage -FolderPath $adminMemoPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/System/Error
$adminSystemErrorPath = Join-Path $webClientPath "Administration\System\Error"
$adminErrorPages = @(
    @{Name="ErrorLog"; Route="/administration/system/error/error-log"; Title="Error Log"; Roles=@("SysAdmin")},
    @{Name="ErrorTest"; Route="/administration/system/error/error-test"; Title="Error Test"; Roles=@("SysAdmin")}
)
foreach ($page in $adminErrorPages) {
    New-RazorPage -FolderPath $adminSystemErrorPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration/System
$adminSystemPath = Join-Path $webClientPath "Administration\System"
$adminSystemPages = @(
    @{Name="HistoricalCopySnapshot"; Route="/administration/system/historical-copy-snapshot"; Title="Historical Copy (Snapshot)"; Roles=@("SysAdmin")},
    @{Name="Rules"; Route="/administration/system/rules"; Title="Rules"; Roles=@("SysAdmin")},
    @{Name="StatusCodes"; Route="/administration/system/status-codes"; Title="Status Codes"; Roles=@("SysAdmin")},
    @{Name="TestAutomaticProcesses"; Route="/administration/system/test-automatic-processes"; Title="Test Automatic Processes"; Roles=@("SysAdmin")},
    @{Name="TestComponent"; Route="/administration/system/test-component"; Title="Test Component"; Roles=@("SysAdmin")},
    @{Name="TestPage"; Route="/administration/system/test-page"; Title="Test Page"; Roles=@("SysAdmin")}
)
foreach ($page in $adminSystemPages) {
    New-RazorPage -FolderPath $adminSystemPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Administration root-level hidden pages
$adminPath = Join-Path $webClientPath "Administration"
$adminRootPages = @(
    @{Name="EditMember"; Route="/administration/edit-member"; Title="Edit Member"; Roles=@("SysAdmin")},
    @{Name="UserPermissions"; Route="/administration/user-permissions"; Title="User Permissions"; Roles=@("SysAdmin")},
    @{Name="UserActivityLog"; Route="/administration/user-activity-log"; Title="User Activity Log"; Roles=@("SysAdmin","ViewUserLog")},
    @{Name="EditTemplate"; Route="/administration/edit-template"; Title="Edit Template"; Roles=@("SysAdmin")},
    @{Name="WorkflowPermissions"; Route="/administration/workflow-permissions"; Title="Workflow Permissions"; Roles=@("SysAdmin")},
    @{Name="WorkflowSteps"; Route="/administration/workflow-steps"; Title="Workflow Steps"; Roles=@("SysAdmin")},
    @{Name="OptionRules"; Route="/administration/option-rules"; Title="Option Rules"; Roles=@("SysAdmin")},
    @{Name="OptionActions"; Route="/administration/option-actions"; Title="Option Actions"; Roles=@("SysAdmin")},
    @{Name="ReminderEmails"; Route="/administration/reminder-emails"; Title="Reminder Emails"; Roles=@("SysAdmin")},
    @{Name="EditUserAccount"; Route="/administration/edit-user-account"; Title="Edit User Account"; Roles=@("UsersEdit","SysAdmin","UsersApprove")},
    @{Name="PrintAccountHistory"; Route="/administration/print-account-history"; Title="Print Account History"; Roles=@("UsersEdit","SysAdmin","UsersApprove")}
)
foreach ($page in $adminRootPages) {
    New-RazorPage -FolderPath $adminPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

Write-Host "`nPart 2 script execution complete!" -ForegroundColor Green
