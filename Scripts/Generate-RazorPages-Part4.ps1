# Part 4: OtherCases Workflow Pages Generation Script
# This continues from Generate-RazorPages-Part3.ps1

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

# OtherCases/WWD
Write-Host "`nCreating OtherCases/WWD workflow pages..." -ForegroundColor Cyan
$wwdPath = Join-Path $webClientPath "OtherCases\WWD"
$wwdPages = @(
    @{Name="Init"; Route="/other-cases/wwd/init"; Title="WWD Initialization"; Roles=@("WWDView")},
    @{Name="WDMember"; Route="/other-cases/wwd/wd-member"; Title="WD Member"; Roles=@("WWDView")},
    @{Name="WDMedTech"; Route="/other-cases/wwd/wd-med-tech"; Title="WD Med Tech"; Roles=@("WWDView")},
    @{Name="WDHQAFRCTech"; Route="/other-cases/wwd/wd-hq-afrc-tech"; Title="WD HQ AFRC Tech"; Roles=@("WWDView")},
    @{Name="WDBoardMed"; Route="/other-cases/wwd/wd-board-med"; Title="WD Board Med"; Roles=@("WWDView")},
    @{Name="WDDocuments"; Route="/other-cases/wwd/wd-documents"; Title="WD Documents"; Roles=@("WWDView")},
    @{Name="WDNextAction"; Route="/other-cases/wwd/wd-next-action"; Title="WD Next Action"; Roles=@("WWDView")},
    @{Name="WDCaseComments"; Route="/other-cases/wwd/wd-case-comments"; Title="WD Case Comments"; Roles=@("WWDView")},
    @{Name="WDTracking"; Route="/other-cases/wwd/wd-tracking"; Title="WD Tracking"; Roles=@("WWDView")},
    @{Name="Print"; Route="/other-cases/wwd/print"; Title="WWD Print"; Roles=@("WWDView")},
    @{Name="CaseDialogue"; Route="/other-cases/wwd/case-dialogue"; Title="WWD Case Dialogue"; Roles=@("WWDView")}
)
foreach ($page in $wwdPages) {
    New-RazorPage -FolderPath $wwdPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/PWaiver
Write-Host "`nCreating OtherCases/PWaiver workflow pages..." -ForegroundColor Cyan
$pwPath = Join-Path $webClientPath "OtherCases\PWaiver"
$pwPages = @(
    @{Name="Init"; Route="/other-cases/pwaiver/init"; Title="PWaiver Initialization"; Roles=@("PWView")},
    @{Name="PWMember"; Route="/other-cases/pwaiver/pw-member"; Title="PW Member"; Roles=@("PWView")},
    @{Name="PWMedTech"; Route="/other-cases/pwaiver/pw-med-tech"; Title="PW Med Tech"; Roles=@("PWView")},
    @{Name="PWBoardMed"; Route="/other-cases/pwaiver/pw-board-med"; Title="PW Board Med"; Roles=@("PWView")},
    @{Name="PWDocuments"; Route="/other-cases/pwaiver/pw-documents"; Title="PW Documents"; Roles=@("PWView")},
    @{Name="PWNextAction"; Route="/other-cases/pwaiver/pw-next-action"; Title="PW Next Action"; Roles=@("PWView")},
    @{Name="PWCaseComments"; Route="/other-cases/pwaiver/pw-case-comments"; Title="PW Case Comments"; Roles=@("PWView")},
    @{Name="PWTracking"; Route="/other-cases/pwaiver/pw-tracking"; Title="PW Tracking"; Roles=@("PWView")},
    @{Name="Print"; Route="/other-cases/pwaiver/print"; Title="PWaiver Print"; Roles=@("PWView")},
    @{Name="CaseDialogue"; Route="/other-cases/pwaiver/case-dialogue"; Title="PWaiver Case Dialogue"; Roles=@("PWView")}
)
foreach ($page in $pwPages) {
    New-RazorPage -FolderPath $pwPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/AGRCert
Write-Host "`nCreating OtherCases/AGRCert workflow pages..." -ForegroundColor Cyan
$agrPath = Join-Path $webClientPath "OtherCases\AGRCert"
$agrPages = @(
    @{Name="Init"; Route="/other-cases/agr-cert/init"; Title="AGR Cert Initialization"; Roles=@("AGRCertView")},
    @{Name="AGRMember"; Route="/other-cases/agr-cert/agr-member"; Title="AGR Member"; Roles=@("AGRCertView")},
    @{Name="AGRMedTech"; Route="/other-cases/agr-cert/agr-med-tech"; Title="AGR Med Tech"; Roles=@("AGRCertView")},
    @{Name="AGRMedicalReview"; Route="/other-cases/agr-cert/agr-medical-review"; Title="AGR Medical Review"; Roles=@("AGRCertView")},
    @{Name="AGRDocuments"; Route="/other-cases/agr-cert/agr-documents"; Title="AGR Documents"; Roles=@("AGRCertView")},
    @{Name="AGRNextAction"; Route="/other-cases/agr-cert/agr-next-action"; Title="AGR Next Action"; Roles=@("AGRCertView")},
    @{Name="AGRCaseComments"; Route="/other-cases/agr-cert/agr-case-comments"; Title="AGR Case Comments"; Roles=@("AGRCertView")},
    @{Name="AGRTracking"; Route="/other-cases/agr-cert/agr-tracking"; Title="AGR Tracking"; Roles=@("AGRCertView")},
    @{Name="AGRCaseDialogue"; Route="/other-cases/agr-cert/agr-case-dialogue"; Title="AGR Case Dialogue"; Roles=@("AGRCertView")}
)
foreach ($page in $agrPages) {
    New-RazorPage -FolderPath $agrPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/MEB
Write-Host "`nCreating OtherCases/MEB workflow pages..." -ForegroundColor Cyan
$mebPath = Join-Path $webClientPath "OtherCases\MEB"
$mebPages = @(
    @{Name="Init"; Route="/other-cases/meb/init"; Title="MEB Initialization"; Roles=@("MEBView")},
    @{Name="MBMember"; Route="/other-cases/meb/mb-member"; Title="MB Member"; Roles=@("MEBView")},
    @{Name="MBHQAFRCTech"; Route="/other-cases/meb/mb-hq-afrc-tech"; Title="MB HQ AFRC Tech"; Roles=@("MEBView")},
    @{Name="MBMedOff"; Route="/other-cases/meb/mb-med-off"; Title="MB Med Off"; Roles=@("MEBView")},
    @{Name="MBMedTech"; Route="/other-cases/meb/mb-med-tech"; Title="MB Med Tech"; Roles=@("MEBView")},
    @{Name="MBDocuments"; Route="/other-cases/meb/mb-documents"; Title="MB Documents"; Roles=@("MEBView")},
    @{Name="MBNextAction"; Route="/other-cases/meb/mb-next-action"; Title="MB Next Action"; Roles=@("MEBView")},
    @{Name="MBCaseComments"; Route="/other-cases/meb/mb-case-comments"; Title="MB Case Comments"; Roles=@("MEBView")},
    @{Name="MBTracking"; Route="/other-cases/meb/mb-tracking"; Title="MB Tracking"; Roles=@("MEBView")},
    @{Name="Print"; Route="/other-cases/meb/print"; Title="MEB Print"; Roles=@("MEBView")},
    @{Name="CaseDialogue"; Route="/other-cases/meb/case-dialogue"; Title="MEB Case Dialogue"; Roles=@("MEBView")}
)
foreach ($page in $mebPages) {
    New-RazorPage -FolderPath $mebPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/INCAP
Write-Host "`nCreating OtherCases/SpecialtyCases/INCAP workflow pages..." -ForegroundColor Cyan
$incapPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\INCAP"
$incapPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/incap/init"; Title="INCAP Initialization"; Roles=@("INCAPView")},
    @{Name="INMember"; Route="/other-cases/specialty-cases/incap/in-member"; Title="IN Member"; Roles=@("INCAPView")},
    @{Name="INWingInt"; Route="/other-cases/specialty-cases/incap/in-wing-int"; Title="IN Wing Int"; Roles=@("INCAPView")},
    @{Name="INHQExtApp"; Route="/other-cases/specialty-cases/incap/in-hq-ext-app"; Title="IN HQ Ext/App"; Roles=@("INCAPView")},
    @{Name="INDocuments"; Route="/other-cases/specialty-cases/incap/in-documents"; Title="IN Documents"; Roles=@("INCAPView")},
    @{Name="LODDocuments"; Route="/other-cases/specialty-cases/incap/lod-documents"; Title="LOD Documents"; Roles=@("INCAPView")},
    @{Name="INNextAction"; Route="/other-cases/specialty-cases/incap/in-next-action"; Title="IN Next Action"; Roles=@("INCAPView")},
    @{Name="INCaseComments"; Route="/other-cases/specialty-cases/incap/in-case-comments"; Title="IN Case Comments"; Roles=@("INCAPView")},
    @{Name="INTracking"; Route="/other-cases/specialty-cases/incap/in-tracking"; Title="IN Tracking"; Roles=@("INCAPView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/incap/print"; Title="INCAP Print"; Roles=@("INCAPView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/incap/case-dialogue"; Title="INCAP Case Dialogue"; Roles=@("INCAPView")}
)
foreach ($page in $incapPages) {
    New-RazorPage -FolderPath $incapPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/BMTWaiversMEPS
Write-Host "`nCreating OtherCases/SpecialtyCases/BMTWaiversMEPS workflow pages..." -ForegroundColor Cyan
$bmtPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\BMTWaiversMEPS"
$bmtPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/bmt-waivers-meps/init"; Title="BMT Waivers/MEPS Initialization"; Roles=@("BMTView")},
    @{Name="BTMember"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-member"; Title="BT Member"; Roles=@("BMTView")},
    @{Name="BTBoardMed"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-board-med"; Title="BT Board Med"; Roles=@("BMTView")},
    @{Name="BTHQAFRCTech"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-hq-afrc-tech"; Title="BT HQ AFRC Tech"; Roles=@("BMTView")},
    @{Name="BTDocuments"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-documents"; Title="BT Documents"; Roles=@("BMTView")},
    @{Name="BTBoardReview"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-board-review"; Title="BT Board Review"; Roles=@("BMTView")},
    @{Name="BTNextAction"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-next-action"; Title="BT Next Action"; Roles=@("BMTView")},
    @{Name="BTCaseComments"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-case-comments"; Title="BT Case Comments"; Roles=@("BMTView")},
    @{Name="BTTracking"; Route="/other-cases/specialty-cases/bmt-waivers-meps/bt-tracking"; Title="BT Tracking"; Roles=@("BMTView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/bmt-waivers-meps/print"; Title="BMT Print"; Roles=@("BMTView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/bmt-waivers-meps/case-dialogue"; Title="BMT Case Dialogue"; Roles=@("BMTView")}
)
foreach ($page in $bmtPages) {
    New-RazorPage -FolderPath $bmtPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/Congressionals
Write-Host "`nCreating OtherCases/SpecialtyCases/Congressionals workflow pages..." -ForegroundColor Cyan
$ciPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\Congressionals"
$ciPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/congressionals/init"; Title="Congressional Initialization"; Roles=@("CIView")},
    @{Name="CIMember"; Route="/other-cases/specialty-cases/congressionals/ci-member"; Title="CI Member"; Roles=@("CIView")},
    @{Name="CIMedTech"; Route="/other-cases/specialty-cases/congressionals/ci-med-tech"; Title="CI Med Tech"; Roles=@("CIView")},
    @{Name="CIHQAFRCTech"; Route="/other-cases/specialty-cases/congressionals/ci-hq-afrc-tech"; Title="CI HQ AFRC Tech"; Roles=@("CIView")},
    @{Name="CIDocuments"; Route="/other-cases/specialty-cases/congressionals/ci-documents"; Title="CI Documents"; Roles=@("CIView")},
    @{Name="CINextAction"; Route="/other-cases/specialty-cases/congressionals/ci-next-action"; Title="CI Next Action"; Roles=@("CIView")},
    @{Name="CICaseComments"; Route="/other-cases/specialty-cases/congressionals/ci-case-comments"; Title="CI Case Comments"; Roles=@("CIView")},
    @{Name="CITracking"; Route="/other-cases/specialty-cases/congressionals/ci-tracking"; Title="CI Tracking"; Roles=@("CIView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/congressionals/print"; Title="Congressional Print"; Roles=@("CIView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/congressionals/case-dialogue"; Title="Congressional Case Dialogue"; Roles=@("CIView")}
)
foreach ($page in $ciPages) {
    New-RazorPage -FolderPath $ciPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/BCMR
Write-Host "`nCreating OtherCases/SpecialtyCases/BCMR workflow pages..." -ForegroundColor Cyan
$bcmrPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\BCMR"
$bcmrPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/bcmr/init"; Title="BCMR Initialization"; Roles=@("BCMRView")},
    @{Name="BCMember"; Route="/other-cases/specialty-cases/bcmr/bc-member"; Title="BC Member"; Roles=@("BCMRView")},
    @{Name="BCMedTech"; Route="/other-cases/specialty-cases/bcmr/bc-med-tech"; Title="BC Med Tech"; Roles=@("BCMRView")},
    @{Name="BCHQAFRCTech"; Route="/other-cases/specialty-cases/bcmr/bc-hq-afrc-tech"; Title="BC HQ AFRC Tech"; Roles=@("BCMRView")},
    @{Name="BCDocuments"; Route="/other-cases/specialty-cases/bcmr/bc-documents"; Title="BC Documents"; Roles=@("BCMRView")},
    @{Name="BCNextAction"; Route="/other-cases/specialty-cases/bcmr/bc-next-action"; Title="BC Next Action"; Roles=@("BCMRView")},
    @{Name="BCCaseComments"; Route="/other-cases/specialty-cases/bcmr/bc-case-comments"; Title="BC Case Comments"; Roles=@("BCMRView")},
    @{Name="BCTracking"; Route="/other-cases/specialty-cases/bcmr/bc-tracking"; Title="BC Tracking"; Roles=@("BCMRView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/bcmr/print"; Title="BCMR Print"; Roles=@("BCMRView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/bcmr/case-dialogue"; Title="BCMR Case Dialogue"; Roles=@("BCMRView")}
)
foreach ($page in $bcmrPages) {
    New-RazorPage -FolderPath $bcmrPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/CMAS
Write-Host "`nCreating OtherCases/SpecialtyCases/CMAS workflow pages..." -ForegroundColor Cyan
$cmasPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\CMAS"
$cmasPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/cmas/init"; Title="CMAS Initialization"; Roles=@("CMASView")},
    @{Name="CMMember"; Route="/other-cases/specialty-cases/cmas/cm-member"; Title="CM Member"; Roles=@("CMASView")},
    @{Name="CMMedTech"; Route="/other-cases/specialty-cases/cmas/cm-med-tech"; Title="CM Med Tech"; Roles=@("CMASView")},
    @{Name="CMHQAFRCTech"; Route="/other-cases/specialty-cases/cmas/cm-hq-afrc-tech"; Title="CM HQ AFRC Tech"; Roles=@("CMASView")},
    @{Name="CMDocuments"; Route="/other-cases/specialty-cases/cmas/cm-documents"; Title="CM Documents"; Roles=@("CMASView")},
    @{Name="CMNextAction"; Route="/other-cases/specialty-cases/cmas/cm-next-action"; Title="CM Next Action"; Roles=@("CMASView")},
    @{Name="CMCaseComments"; Route="/other-cases/specialty-cases/cmas/cm-case-comments"; Title="CM Case Comments"; Roles=@("CMASView")},
    @{Name="CMTracking"; Route="/other-cases/specialty-cases/cmas/cm-tracking"; Title="CM Tracking"; Roles=@("CMASView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/cmas/print"; Title="CMAS Print"; Roles=@("CMASView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/cmas/case-dialogue"; Title="CMAS Case Dialogue"; Roles=@("CMASView")}
)
foreach ($page in $cmasPages) {
    New-RazorPage -FolderPath $cmasPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/IRILO
Write-Host "`nCreating OtherCases/SpecialtyCases/IRILO workflow pages..." -ForegroundColor Cyan
$iriloPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\IRILO"
$iriloPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/irilo/init"; Title="IRILO Initialization"; Roles=@("IRILOView")},
    @{Name="IRMember"; Route="/other-cases/specialty-cases/irilo/ir-member"; Title="IR Member"; Roles=@("IRILOView")},
    @{Name="IRMedTech"; Route="/other-cases/specialty-cases/irilo/ir-med-tech"; Title="IR Med Tech"; Roles=@("IRILOView")},
    @{Name="IRHQAFRCTech"; Route="/other-cases/specialty-cases/irilo/ir-hq-afrc-tech"; Title="IR HQ AFRC Tech"; Roles=@("IRILOView")},
    @{Name="IRMedOff"; Route="/other-cases/specialty-cases/irilo/ir-med-off"; Title="IR Med Off"; Roles=@("IRILOView")},
    @{Name="IRDocuments"; Route="/other-cases/specialty-cases/irilo/ir-documents"; Title="IR Documents"; Roles=@("IRILOView")},
    @{Name="LODDocuments"; Route="/other-cases/specialty-cases/irilo/lod-documents"; Title="LOD Documents"; Roles=@("IRILOView")},
    @{Name="IRNextAction"; Route="/other-cases/specialty-cases/irilo/ir-next-action"; Title="IR Next Action"; Roles=@("IRILOView")},
    @{Name="IRCaseComments"; Route="/other-cases/specialty-cases/irilo/ir-case-comments"; Title="IR Case Comments"; Roles=@("IRILOView")},
    @{Name="IRTracking"; Route="/other-cases/specialty-cases/irilo/ir-tracking"; Title="IR Tracking"; Roles=@("IRILOView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/irilo/print"; Title="IRILO Print"; Roles=@("IRILOView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/irilo/case-dialogue"; Title="IRILO Case Dialogue"; Roles=@("IRILOView")}
)
foreach ($page in $iriloPages) {
    New-RazorPage -FolderPath $iriloPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/MMSO
Write-Host "`nCreating OtherCases/SpecialtyCases/MMSO workflow pages..." -ForegroundColor Cyan
$mmsoPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\MMSO"
$mmsoPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/mmso/init"; Title="MMSO Initialization"; Roles=@("*")},
    @{Name="MMMember"; Route="/other-cases/specialty-cases/mmso/mm-member"; Title="MM Member"; Roles=@("*")},
    @{Name="PreAuthorizationRequest"; Route="/other-cases/specialty-cases/mmso/pre-authorization-request"; Title="Pre-Authorization Request"; Roles=@("*")},
    @{Name="MMDocuments"; Route="/other-cases/specialty-cases/mmso/mm-documents"; Title="MM Documents"; Roles=@("*")},
    @{Name="MMLODDocuments"; Route="/other-cases/specialty-cases/mmso/mm-lod-documents"; Title="MM LOD Documents"; Roles=@("*")},
    @{Name="MMNextAction"; Route="/other-cases/specialty-cases/mmso/mm-next-action"; Title="MM Next Action"; Roles=@("*")},
    @{Name="MMTracking"; Route="/other-cases/specialty-cases/mmso/mm-tracking"; Title="MM Tracking"; Roles=@("*")},
    @{Name="Print"; Route="/other-cases/specialty-cases/mmso/print"; Title="MMSO Print"; Roles=@("*")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/mmso/case-dialogue"; Title="MMSO Case Dialogue"; Roles=@("*")}
)
foreach ($page in $mmsoPages) {
    New-RazorPage -FolderPath $mmsoPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/MH
Write-Host "`nCreating OtherCases/SpecialtyCases/MH workflow pages..." -ForegroundColor Cyan
$mhPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\MH"
$mhPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/mh/init"; Title="MH Initialization"; Roles=@("MHView")},
    @{Name="MHMember"; Route="/other-cases/specialty-cases/mh/mh-member"; Title="MH Member"; Roles=@("MHView")},
    @{Name="MHHQAFRCTech"; Route="/other-cases/specialty-cases/mh/mh-hq-afrc-tech"; Title="MH HQ AFRC Tech"; Roles=@("MHView")},
    @{Name="MHBoardMed"; Route="/other-cases/specialty-cases/mh/mh-board-med"; Title="MH Board Med"; Roles=@("MHView")},
    @{Name="MHDocuments"; Route="/other-cases/specialty-cases/mh/mh-documents"; Title="MH Documents"; Roles=@("MHView")},
    @{Name="MHNextAction"; Route="/other-cases/specialty-cases/mh/mh-next-action"; Title="MH Next Action"; Roles=@("MHView")},
    @{Name="MHCaseComments"; Route="/other-cases/specialty-cases/mh/mh-case-comments"; Title="MH Case Comments"; Roles=@("MHView")},
    @{Name="MHTracking"; Route="/other-cases/specialty-cases/mh/mh-tracking"; Title="MH Tracking"; Roles=@("MHView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/mh/print"; Title="MH Print"; Roles=@("MHView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/mh/case-dialogue"; Title="MH Case Dialogue"; Roles=@("MHView")}
)
foreach ($page in $mhPages) {
    New-RazorPage -FolderPath $mhPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

Write-Host "`nPart 4 script execution complete!" -ForegroundColor Green
