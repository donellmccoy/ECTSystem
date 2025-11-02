# Part 3: Help, Root Pages, and Workflow Pages Generation Script
# This continues from Generate-RazorPages-Part2.ps1

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

# Help Pages
Write-Host "`nCreating Help pages..." -ForegroundColor Cyan
$helpPath = Join-Path $webClientPath "Help"
$helpPages = @(
    @{Name="SelectAccount"; Route="/help/select-account"; Title="Select Account"; Roles=@("*")},
    @{Name="MyAccount"; Route="/help/my-account"; Title="My Account"; Roles=@("*")}
)
foreach ($page in $helpPages) {
    New-RazorPage -FolderPath $helpPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Help/Documentation Pages
$helpDocPath = Join-Path $webClientPath "Help\Documentation"
$helpDocPages = @(
    @{Name="ALODManual"; Route="/help/documentation/alod-manual"; Title="ALOD Manual"},
    @{Name="MasterLODChecklist"; Route="/help/documentation/master-lod-checklist"; Title="Master LOD Checklist"},
    @{Name="EightYearRule"; Route="/help/documentation/8-year-rule"; Title="8 Year Rule"},
    @{Name="AFI36_2910AFRCSUP1I"; Route="/help/documentation/afi-36-2910-afrcsup1-i"; Title="AFI 36-2910 AFRCSUP1 I"},
    @{Name="AFRCI36_3004"; Route="/help/documentation/afrci-36-3004"; Title="AFRCI 36-3004"},
    @{Name="DODI1241_2"; Route="/help/documentation/dodi-1241-2"; Title="DODI 1241.2"},
    @{Name="DODI1332_38"; Route="/help/documentation/dodi-1332-38"; Title="DODI 1332.38"},
    @{Name="SARCModifications"; Route="/help/documentation/sarc-modifications"; Title="SARC Modifications"},
    @{Name="MedicalBriefing"; Route="/help/documentation/medical-briefing"; Title="Medical Briefing"},
    @{Name="PersonnelBriefing"; Route="/help/documentation/personnel-briefing"; Title="Personnel Briefing"},
    @{Name="FinancialEntitlementsBriefing"; Route="/help/documentation/financial-entitlements-briefing"; Title="Financial Entitlements Briefing"},
    @{Name="LODPolicyForARCMembers"; Route="/help/documentation/lod-policy-for-arc-members"; Title="LOD Policy for ARC Members"},
    @{Name="AFRCPHAGuideMar2015"; Route="/help/documentation/afrc-pha-guide-mar-2015"; Title="AFRC PHA Guide (Mar 2015)"},
    @{Name="AFRCSGPCPMJun15"; Route="/help/documentation/afrc-sgp-cpm-1-jun-15"; Title="AFRC SGP CPM (1 Jun 15)"},
    @{Name="ALODRTM"; Route="/help/documentation/alod-rtm"; Title="ALOD RTM"},
    @{Name="UserAccount"; Route="/help/documentation/user-account"; Title="User Account"},
    @{Name="AdHocReporting"; Route="/help/documentation/ad-hoc-reporting"; Title="Ad Hoc Reporting"},
    @{Name="LODCategoryCountReport"; Route="/help/documentation/lod-category-count-report"; Title="LOD Category Count Report"},
    @{Name="LODDisapprovedReport"; Route="/help/documentation/lod-dispapproved-report"; Title="LOD Disapproved Report"},
    @{Name="LODDispositionReport"; Route="/help/documentation/lod-disposition-report"; Title="LOD Disposition Report"},
    @{Name="LODMetricsReport"; Route="/help/documentation/lod-metrics-report"; Title="LOD Metrics Report"},
    @{Name="LODPhysicianCancelledReport"; Route="/help/documentation/lod-physician-cancelled-report"; Title="LOD Physician Cancelled Report"},
    @{Name="LODRWOAReport"; Route="/help/documentation/lod-rwoa-report"; Title="LOD RWOA Report"},
    @{Name="LODStatisticsReport"; Route="/help/documentation/lod-statistics-report"; Title="LOD Statistics Report"},
    @{Name="LODTotalCountReport"; Route="/help/documentation/lod-total-count-report"; Title="LOD Total Count Report"},
    @{Name="InvestigationOfficer"; Route="/help/documentation/investigation-officer"; Title="Investigation Officer"},
    @{Name="MedicalOfficer"; Route="/help/documentation/medical-officer"; Title="Medical Officer"},
    @{Name="MedicalTechnician"; Route="/help/documentation/medical-technician"; Title="Medical Technician"},
    @{Name="MPF"; Route="/help/documentation/mpf"; Title="MPF"},
    @{Name="UnitCommander"; Route="/help/documentation/unit-commander"; Title="Unit Commander"},
    @{Name="WingCommander"; Route="/help/documentation/wing-commander"; Title="Wing Commander"},
    @{Name="WingJA"; Route="/help/documentation/wing-ja"; Title="Wing JA"},
    @{Name="WingSARC"; Route="/help/documentation/wing-sarc"; Title="Wing SARC"}
)
foreach ($page in $helpDocPages) {
    New-RazorPage -FolderPath $helpDocPath -FileName $page.Name -Route $page.Route -Roles @("*") -PageTitle $page.Title
}

# Release Notes
$releaseNotesPath = Join-Path $webClientPath "Help\Documentation\ReleaseNotes"
$releaseVersions = @("1.2.4", "1.2.3", "1.2.2", "1.2.1", "1.2.0", "1.1.11", "1.1.10.2", "1.1.9", "1.1.7", "1.1.6", "1.1.5", "1.1.4.1", "1.1.4", "1.1.3", "1.1.1", "1.1.0", "1.0.10", "1.0.9")
foreach ($version in $releaseVersions) {
    $versionClean = $version.Replace(".", "_")
    New-RazorPage -FolderPath $releaseNotesPath -FileName "ReleaseNotesV$versionClean" -Route "/help/documentation/release-notes-v$($version.Replace('.', '-'))" -Roles @("*") -PageTitle "Release Notes - v$version"
}

# Additional documentation pages
$additionalDocs = @(
    @{Name="AFRCALODInstrFor1_1_11"; Route="/help/documentation/afrc-alod-instr-for-1-1-11"; Title="AFRC ALOD Instr for 1.1.11"},
    @{Name="AFRCALODInstrFor1_1_10_2"; Route="/help/documentation/afrc-alod-instr-for-1-1-10-2"; Title="AFRC ALOD Instr for 1.1.10.2"}
)
foreach ($page in $additionalDocs) {
    New-RazorPage -FolderPath $helpDocPath -FileName $page.Name -Route $page.Route -Roles @("*") -PageTitle $page.Title
}

# Root-level Pages
Write-Host "`nCreating root-level pages..." -ForegroundColor Cyan
$rootPages = @(
    @{Name="Edit"; Route="/edit"; Title="Edit"; Roles=@("*")},
    @{Name="SearchUnit"; Route="/search-unit"; Title="Search Unit"; Roles=@("*")},
    @{Name="UnitAdministration"; Route="/unit-administration"; Title="Unit Administration"; Roles=@("*")}
)
foreach ($page in $rootPages) {
    New-RazorPage -FolderPath $webClientPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Line Of Duty Workflow Pages
Write-Host "`nCreating Line Of Duty workflow pages..." -ForegroundColor Cyan
$lodWorkflowPath = Join-Path $webClientPath "LineOfDuty"
$lodWorkflowPages = @(
    @{Name="Init"; Route="/line-of-duty/init"; Title="LOD Initialization"; Roles=@("LodView")},
    @{Name="Member"; Route="/line-of-duty/member"; Title="LOD Member"; Roles=@("LodView")},
    @{Name="UnitCC"; Route="/line-of-duty/unit-cc"; Title="LOD Unit CC"; Roles=@("LodView")},
    @{Name="Medical"; Route="/line-of-duty/medical"; Title="LOD Medical"; Roles=@("LodView")},
    @{Name="Audit"; Route="/line-of-duty/audit"; Title="LOD Audit"; Roles=@("LodView")},
    @{Name="WingJA"; Route="/line-of-duty/wing-ja"; Title="LOD Wing JA"; Roles=@("LodView")},
    @{Name="WingCC"; Route="/line-of-duty/wing-cc"; Title="LOD Wing CC"; Roles=@("LodView")},
    @{Name="SeniorMed"; Route="/line-of-duty/senior-med"; Title="LOD Senior Med"; Roles=@("LodView")},
    @{Name="Investigation"; Route="/line-of-duty/investigation"; Title="LOD Investigation"; Roles=@("LodView")},
    @{Name="Documents"; Route="/line-of-duty/documents"; Title="LOD Documents"; Roles=@("LodView")},
    @{Name="LODBoard"; Route="/line-of-duty/lod-board"; Title="LOD Board"; Roles=@("LodView")},
    @{Name="NextAction"; Route="/line-of-duty/next-action"; Title="LOD Next Action"; Roles=@("LodView")},
    @{Name="Tracking"; Route="/line-of-duty/tracking"; Title="LOD Tracking"; Roles=@("LodView")},
    @{Name="Print"; Route="/line-of-duty/print"; Title="LOD Print"; Roles=@("LodView")},
    @{Name="LessonsLearned"; Route="/line-of-duty/lessons-learned"; Title="LOD Lessons Learned"; Roles=@("LodView")},
    @{Name="Comments"; Route="/line-of-duty/comments"; Title="LOD Comments"; Roles=@("LodView")},
    @{Name="CaseDialogue"; Route="/line-of-duty/case-dialogue"; Title="LOD Case Dialogue"; Roles=@("LodView")}
)
foreach ($page in $lodWorkflowPages) {
    New-RazorPage -FolderPath $lodWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# Reinvestigation Request Workflow Pages
Write-Host "`nCreating Reinvestigation Request workflow pages..." -ForegroundColor Cyan
$rrWorkflowPath = Join-Path $webClientPath "ReinvestigationRequest"
$rrWorkflowPages = @(
    @{Name="Init"; Route="/reinvestigation-request/init"; Title="RR Initialization"; Roles=@("RRView")},
    @{Name="RRMember"; Route="/reinvestigation-request/rr-member"; Title="RR Member"; Roles=@("RRView")},
    @{Name="RRWingJA"; Route="/reinvestigation-request/rr-wing-ja"; Title="RR Wing JA"; Roles=@("RRView")},
    @{Name="RRWingCC"; Route="/reinvestigation-request/rr-wing-cc"; Title="RR Wing CC"; Roles=@("RRView")},
    @{Name="RRSeniorMed"; Route="/reinvestigation-request/rr-senior-med"; Title="RR Senior Med"; Roles=@("RRView")},
    @{Name="RRDocuments"; Route="/reinvestigation-request/rr-documents"; Title="RR Documents"; Roles=@("RRView")},
    @{Name="RRBoardReview"; Route="/reinvestigation-request/rr-board-review"; Title="RR Board Review"; Roles=@("RRView")},
    @{Name="RRNextAction"; Route="/reinvestigation-request/rr-next-action"; Title="RR Next Action"; Roles=@("RRView")},
    @{Name="RRCaseComments"; Route="/reinvestigation-request/rr-case-comments"; Title="RR Case Comments"; Roles=@("RRView")},
    @{Name="RRTracking"; Route="/reinvestigation-request/rr-tracking"; Title="RR Tracking"; Roles=@("RRView")},
    @{Name="Print"; Route="/reinvestigation-request/print"; Title="RR Print"; Roles=@("RRView")},
    @{Name="CaseDialogue"; Route="/reinvestigation-request/case-dialogue"; Title="RR Case Dialogue"; Roles=@("RRView")}
)
foreach ($page in $rrWorkflowPages) {
    New-RazorPage -FolderPath $rrWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# Appeal Workflow Pages
Write-Host "`nCreating Appeal workflow pages..." -ForegroundColor Cyan
$appealWorkflowPath = Join-Path $webClientPath "Appeal"
$appealWorkflowPages = @(
    @{Name="Init"; Route="/appeal/init"; Title="Appeal Initialization"; Roles=@("APView")},
    @{Name="APMember"; Route="/appeal/ap-member"; Title="Appeal Member"; Roles=@("APView")},
    @{Name="LODDocuments"; Route="/appeal/lod-documents"; Title="LOD Documents"; Roles=@("APView")},
    @{Name="APDocuments"; Route="/appeal/ap-documents"; Title="Appeal Documents"; Roles=@("APView")},
    @{Name="APSeniorMed"; Route="/appeal/ap-senior-med"; Title="Appeal Senior Med"; Roles=@("APView")},
    @{Name="APBoard"; Route="/appeal/ap-board"; Title="Appeal Board"; Roles=@("APView")},
    @{Name="APNextAction"; Route="/appeal/ap-next-action"; Title="Appeal Next Action"; Roles=@("APView")},
    @{Name="APCaseComments"; Route="/appeal/ap-case-comments"; Title="Appeal Case Comments"; Roles=@("APView")},
    @{Name="APTracking"; Route="/appeal/ap-tracking"; Title="Appeal Tracking"; Roles=@("APView")},
    @{Name="Print"; Route="/appeal/print"; Title="Appeal Print"; Roles=@("APView")},
    @{Name="CaseDialogue"; Route="/appeal/case-dialogue"; Title="Appeal Case Dialogue"; Roles=@("APView")}
)
foreach ($page in $appealWorkflowPages) {
    New-RazorPage -FolderPath $appealWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# Restricted SARC Workflow Pages
Write-Host "`nCreating Restricted SARC workflow pages..." -ForegroundColor Cyan
$rsarcWorkflowPath = Join-Path $webClientPath "RestrictedSARC"
$rsarcWorkflowPages = @(
    @{Name="Init"; Route="/restricted-sarc/init"; Title="SARC Initialization"; Roles=@("RSARCView")},
    @{Name="SARCMember"; Route="/restricted-sarc/sarc-member"; Title="SARC Member"; Roles=@("RSARCView")},
    @{Name="WingSARC"; Route="/restricted-sarc/wing-sarc"; Title="Wing SARC"; Roles=@("RSARCView")},
    @{Name="SARCAdmin"; Route="/restricted-sarc/sarc-admin"; Title="SARC Admin"; Roles=@("RSARCView")},
    @{Name="SARCSeniorMed"; Route="/restricted-sarc/sarc-senior-med"; Title="SARC Senior Med"; Roles=@("RSARCView")},
    @{Name="SARCBoard"; Route="/restricted-sarc/sarc-board"; Title="SARC Board"; Roles=@("RSARCView")},
    @{Name="SARCDocuments"; Route="/restricted-sarc/sarc-documents"; Title="SARC Documents"; Roles=@("RSARCView")},
    @{Name="SARCTracking"; Route="/restricted-sarc/sarc-tracking"; Title="SARC Tracking"; Roles=@("RSARCView")},
    @{Name="SARCCaseComments"; Route="/restricted-sarc/sarc-case-comments"; Title="SARC Case Comments"; Roles=@("RSARCView")},
    @{Name="SARCNextAction"; Route="/restricted-sarc/sarc-next-action"; Title="SARC Next Action"; Roles=@("RSARCView")},
    @{Name="SARCPrint"; Route="/restricted-sarc/sarc-print"; Title="SARC Print"; Roles=@("RSARCView")},
    @{Name="CaseDialogue"; Route="/restricted-sarc/case-dialogue"; Title="SARC Case Dialogue"; Roles=@("RSARCView")}
)
foreach ($page in $rsarcWorkflowPages) {
    New-RazorPage -FolderPath $rsarcWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# SARC Appeal Workflow Pages
Write-Host "`nCreating SARC Appeal workflow pages..." -ForegroundColor Cyan
$sarcAppealWorkflowPath = Join-Path $webClientPath "SARCAppeal"
$sarcAppealWorkflowPages = @(
    @{Name="Init"; Route="/sarc-appeal/init"; Title="SARC Appeal Initialization"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPMember"; Route="/sarc-appeal/sarc-ap-member"; Title="SARC Appeal Member"; Roles=@("RSARCAppealView")},
    @{Name="SARCDocuments"; Route="/sarc-appeal/sarc-documents"; Title="SARC Documents"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPDocuments"; Route="/sarc-appeal/sarc-ap-documents"; Title="SARC Appeal Documents"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPAdmin"; Route="/sarc-appeal/sarc-ap-admin"; Title="SARC Appeal Admin"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPSeniorMed"; Route="/sarc-appeal/sarc-ap-senior-med"; Title="SARC Appeal Senior Med"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPBoard"; Route="/sarc-appeal/sarc-ap-board"; Title="SARC Appeal Board"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPNextAction"; Route="/sarc-appeal/sarc-ap-next-action"; Title="SARC Appeal Next Action"; Roles=@("RSARCAppealView")},
    @{Name="SARCAPCaseComments"; Route="/sarc-appeal/sarc-ap-case-comments"; Title="SARC Appeal Case Comments"; Roles=@("RSARCAppealView")},
    @{Name="SARCAppealTracking"; Route="/sarc-appeal/sarc-appeal-tracking"; Title="SARC Appeal Tracking"; Roles=@("RSARCAppealView")},
    @{Name="CaseDialogue"; Route="/sarc-appeal/case-dialogue"; Title="SARC Appeal Case Dialogue"; Roles=@("RSARCAppealView")}
)
foreach ($page in $sarcAppealWorkflowPages) {
    New-RazorPage -FolderPath $sarcAppealWorkflowPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

Write-Host "`nPart 3 script execution complete!" -ForegroundColor Green
