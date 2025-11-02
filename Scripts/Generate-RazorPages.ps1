# Script to generate Razor pages and code-behind files from site_access.json structure
# This script creates the complete folder structure with all pages

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

# Common Pages
Write-Host "Creating Common pages..." -ForegroundColor Cyan
$commonPath = Join-Path $webClientPath "Common"
$commonPages = @(
    @{Name="Default"; Route="/common/default"; Title="Default"; Roles=@("*")},
    @{Name="ClientTest"; Route="/common/client-test"; Title="Client Test"; Roles=@("*")},
    @{Name="DevLogin"; Route="/common/dev-login"; Title="Dev Login"; Roles=@("*")},
    @{Name="Start"; Route="/common/start"; Title="Start"; Roles=@("*")},
    @{Name="Logout"; Route="/common/logout"; Title="Logout"; Roles=@("*")},
    @{Name="About"; Route="/common/about"; Title="About"; Roles=@("*")},
    @{Name="SessionViewer"; Route="/common/session-viewer"; Title="Session Viewer"; Roles=@("*")},
    @{Name="EditMemo"; Route="/common/edit-memo"; Title="Edit Memo"; Roles=@("*")},
    @{Name="ViewPdf"; Route="/common/view-pdf"; Title="View PDF"; Roles=@("*")},
    @{Name="ChangeLog"; Route="/common/changelog"; Title="Change Log"; Roles=@("*")},
    @{Name="DocumentViewer"; Route="/common/document-viewer"; Title="Document Viewer"; Roles=@("*")},
    @{Name="DocumentUpload"; Route="/common/document-upload"; Title="Document Upload"; Roles=@("*")},
    @{Name="CustomDocumentUpload"; Route="/common/custom-document-upload"; Title="Custom Document Upload"; Roles=@("*")},
    @{Name="AFIReportExportInstructions"; Route="/common/afi-report-export-instructions"; Title="AFI Report Export Instructions"; Roles=@("*")},
    @{Name="PrintTracking"; Route="/common/print-tracking"; Title="Print Tracking"; Roles=@("*")},
    @{Name="Welcome"; Route="/common/welcome"; Title="Welcome"; Roles=@("*")},
    @{Name="AccessDenied"; Route="/common/access-denied"; Title="Access Denied"; Roles=@("*")},
    @{Name="DigitalSignature"; Route="/common/digital-signature"; Title="Digital Signature"; Roles=@("Signature")},
    @{Name="DigitalSignatureFailed"; Route="/common/digital-signature-failed"; Title="Digital Signature Failed"; Roles=@("Signature")},
    @{Name="DigitalSignatureAccepted"; Route="/common/digital-signature-accepted"; Title="Digital Signature Accepted"; Roles=@("Signature")},
    @{Name="DigitalSignaturePosted"; Route="/common/digital-signature-posted"; Title="Digital Signature Posted"; Roles=@("Signature")},
    @{Name="ClearPreviousSignature"; Route="/common/clear-previous-signature"; Title="Clear Previous Signature"; Roles=@("Signature")},
    @{Name="PrintCaseTracking"; Route="/common/print-case-tracking"; Title="Print Case Tracking"; Roles=@("*")},
    @{Name="Error"; Route="/common/error"; Title="Error"; Roles=@("*")},
    @{Name="ApplicationError"; Route="/common/application-error"; Title="Application Error"; Roles=@("*")}
)

foreach ($page in $commonPages) {
    New-RazorPage -FolderPath $commonPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# LOD Pages
Write-Host "`nCreating LOD pages..." -ForegroundColor Cyan
$lodPath = Join-Path $webClientPath "LOD"
$lodPages = @(
    @{Name="MyLODs"; Route="/lod/my-lods"; Title="My LODs"; Roles=@("MyLod")},
    @{Name="MyLODConsults"; Route="/lod/my-lod-consults"; Title="My LOD Consults"; Roles=@("MyLod")},
    @{Name="SCPendingConsults"; Route="/lod/sc-pending-consults"; Title="SC Pending Consults"; Roles=@("MyLod")},
    @{Name="SCAwaitingConsults"; Route="/lod/sc-awaiting-consults"; Title="SC Awaiting Consults"; Roles=@("MyLod")},
    @{Name="MyLODAudit"; Route="/lod/my-lod-audit"; Title="My LOD Audit"; Roles=@("MyLod")},
    @{Name="StartNewLOD"; Route="/lod/start-new-lod"; Title="Start New LOD"; Roles=@("LodCreate","RSARCCreate","SARCUnrestrictedCreate")},
    @{Name="LODSearch"; Route="/lod/lod-search"; Title="LOD Search"; Roles=@("LodSearch")},
    @{Name="Reinvestigations"; Route="/lod/reinvestigations"; Title="Reinvestigations"; Roles=@("Reinvestigate")},
    @{Name="RRSearch"; Route="/lod/rr-search"; Title="RR Search"; Roles=@("ReinvestigateSearch")},
    @{Name="ProcessCompletedCases"; Route="/lod/process-completed-cases"; Title="Process Completed Cases"; Roles=@("ExePostCompletion")},
    @{Name="ProcessCompletedAppeals"; Route="/lod/process-completed-appeals"; Title="Process Completed Appeals"; Roles=@("APCompletion")},
    @{Name="Appeals"; Route="/lod/appeals"; Title="Appeals"; Roles=@("MyAP")},
    @{Name="APSearch"; Route="/lod/ap-search"; Title="AP Search"; Roles=@("APSearch")},
    @{Name="MySARCs"; Route="/lod/my-sarcs"; Title="My SARCs"; Roles=@("MyRSARC")},
    @{Name="SARCSearch"; Route="/lod/sarc-search"; Title="SARC Search"; Roles=@("RSARCSearch")},
    @{Name="ProcessCompletedRestrictedSARCs"; Route="/lod/process-completed-restricted-sarcs"; Title="Process Completed Restricted SARCs"; Roles=@("RSARCPostCompletion")},
    @{Name="MySARCAppeals"; Route="/lod/my-sarc-appeals"; Title="My SARC Appeals"; Roles=@("MyRSARCAppeal")},
    @{Name="SARCAppealSearch"; Route="/lod/sarc-appeal-search"; Title="SARC Appeal Search"; Roles=@("RSARCAppealSearch")},
    @{Name="ProcessCompletedRestrictedSARCAppeals"; Route="/lod/process-completed-restricted-sarc-appeals"; Title="Process Completed Restricted SARC Appeals"; Roles=@("RSARCAppealPostCompletion")},
    @{Name="InboxView"; Route="/lod/inbox-view"; Title="Inbox View"; Roles=@("SysAdmin")},
    @{Name="Override"; Route="/lod/override"; Title="Override"; Roles=@("SysAdmin")},
    @{Name="GenerateLODForms"; Route="/lod/generate-lod-forms"; Title="Generate LOD Forms"; Roles=@("SysAdmin")},
    @{Name="GenerateSARCForms"; Route="/lod/generate-sarc-forms"; Title="Generate SARC Forms"; Roles=@("SysAdmin")}
)

foreach ($page in $lodPages) {
    New-RazorPage -FolderPath $lodPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Special Cases Pages
Write-Host "`nCreating Special Cases pages..." -ForegroundColor Cyan
$scPath = Join-Path $webClientPath "SpecialCases"
$scPages = @(
    @{Name="MySpecialCases"; Route="/special-cases/my-special-cases"; Title="My Special Cases"; Roles=@("MySCs","MMSOSearch")},
    @{Name="Search"; Route="/special-cases/search"; Title="Search"; Roles=@("ScSearch","ScSearchMT")},
    @{Name="OtherCases"; Route="/special-cases/other-cases"; Title="Other Cases"; Roles=@("ScSearch","ScSearchMT","ScCreate","MySCs","SysAdmin")}
)

foreach ($page in $scPages) {
    New-RazorPage -FolderPath $scPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# Case Type Pages (BMT, BCMR, CMAS, CI, DW, INCAP, IRILO, MEB, MH, MO, NE, PWaiver, AGR, PEPP, PH, RS, RW, WWD, PSCD, MMSO)
Write-Host "`nCreating case type pages..." -ForegroundColor Cyan

$caseTypes = @(
    @{Folder="BMT"; Prefix="bmt"; MyRole="MyBmt"; CreateRole="BmtCreate"; SearchRole="BMTSearch"; Title="BMT/MEPS"},
    @{Folder="BCMR"; Prefix="bcmr"; MyRole="MyBcmr"; CreateRole="BcmrCreate"; SearchRole="BCMRSearch"; Title="BCMR"},
    @{Folder="CMAS"; Prefix="cmas"; MyRole="MyCmas"; CreateRole="CmasCreate"; SearchRole="CMASSearch"; Title="CMAS"},
    @{Folder="CI"; Prefix="ci"; MyRole="MyCi"; CreateRole="CiCreate"; SearchRole="CISearch"; Title="Congressional"},
    @{Folder="DW"; Prefix="dw"; MyRole="MyDW"; CreateRole="DWCreate"; SearchRole="DWSearch"; Title="Deployment Waiver"},
    @{Folder="INCAP"; Prefix="incap"; MyRole="MyIncap"; CreateRole="IncapCreate"; SearchRole="INCAPSearch"; Title="INCAP"},
    @{Folder="IRILO"; Prefix="irilo"; MyRole="MyFTs"; CreateRole="FTCreate"; SearchRole="FTSearch"; Title="IRILO"},
    @{Folder="MEB"; Prefix="meb"; MyRole="MyMeb"; CreateRole="MebCreate"; SearchRole="MEBSearch"; Title="MEB"},
    @{Folder="MH"; Prefix="mh"; MyRole="MyMH"; CreateRole="MHCreate"; SearchRole="MHSearch"; Title="Medical Hold"},
    @{Folder="MO"; Prefix="mo"; MyRole="MyMO"; CreateRole="MOCreate"; SearchRole="MOSearch"; Title="Modification"},
    @{Folder="NE"; Prefix="ne"; MyRole="MyNE"; CreateRole="NECreate"; SearchRole="NESearch"; Title="Non-Emergent Surgery Request"},
    @{Folder="PWaiver"; Prefix="pwaiver"; MyRole="MyPwaiver"; CreateRole="PwaiverCreate"; SearchRole="PWSearch"; Title="Participation Waiver"},
    @{Folder="AGR"; Prefix="agr"; MyRole="MyAGRCert"; CreateRole="AGRCertCreate"; SearchRole="AGRCertSearch"; Title="AGR Certification"},
    @{Folder="PEPP"; Prefix="pepp"; MyRole="MyPEPP"; CreateRole="PEPPCreate"; SearchRole="PEPPSearch"; Title="PEPP/AIMWITS"},
    @{Folder="RS"; Prefix="rs"; MyRole="MyRS"; CreateRole="RSCreate"; SearchRole="RSSearch"; Title="Recruiting Services"},
    @{Folder="RW"; Prefix="rw"; MyRole="MyRW"; CreateRole="RWCreate"; SearchRole="RWSearch"; Title="Retention Waiver"},
    @{Folder="WWD"; Prefix="wwd"; MyRole="MyWWDs"; CreateRole="WWDCreate"; SearchRole="WWDSearch"; Title="Worldwide Duty"},
    @{Folder="PSCD"; Prefix="pscd"; MyRole="MyPSCDs"; CreateRole="PSCDCreate"; SearchRole="PSCDSearch"; Title="Prior Service Condition Determination"}
)

foreach ($caseType in $caseTypes) {
    $casePath = Join-Path $webClientPath $caseType.Folder
    $pages = @(
        @{Name="My$($caseType.Folder)s"; Route="/$($caseType.Prefix)/my-$($caseType.Prefix)s"; Title="My $($caseType.Title)s"; Roles=@($caseType.MyRole)},
        @{Name="StartNew$($caseType.Folder)"; Route="/$($caseType.Prefix)/start-new-$($caseType.Prefix)"; Title="Start New $($caseType.Title)"; Roles=@($caseType.CreateRole)},
        @{Name="Search"; Route="/$($caseType.Prefix)/search"; Title="Search"; Roles=@($caseType.SearchRole)}
    )
    foreach ($page in $pages) {
        New-RazorPage -FolderPath $casePath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
    }
}

# PH with additional page
$phPath = Join-Path $webClientPath "PH"
$phPages = @(
    @{Name="MyPHs"; Route="/ph/my-phs"; Title="My PHs"; Roles=@("MyPH")},
    @{Name="StartNewPH"; Route="/ph/start-new-ph"; Title="Start New PH"; Roles=@("PHCreate")},
    @{Name="Search"; Route="/ph/search"; Title="Search"; Roles=@("PHSearch")},
    @{Name="GeneratePHForm"; Route="/ph/generate-ph-form"; Title="Generate PH Form"; Roles=@("SysAdmin")}
)
foreach ($page in $phPages) {
    New-RazorPage -FolderPath $phPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

# MMSO
$mmsoPath = Join-Path $webClientPath "MMSO"
$mmsoPages = @(
    @{Name="StartNewPreAuthorizationRequest"; Route="/mmso/start-new-pre-authorization-request"; Title="Start New Pre-Authorization Request"; Roles=@("MMSOCreate")},
    @{Name="MyRequests"; Route="/mmso/my-requests"; Title="My Requests"; Roles=@("MMSOView")},
    @{Name="Search"; Route="/mmso/search"; Title="Search"; Roles=@("MMSOSearch")}
)
foreach ($page in $mmsoPages) {
    New-RazorPage -FolderPath $mmsoPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title
}

Write-Host "`nScript execution complete!" -ForegroundColor Green
Write-Host "Generated pages in: $webClientPath" -ForegroundColor Yellow
