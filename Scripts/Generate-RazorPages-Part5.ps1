# Part 5: Remaining OtherCases/SpecialtyCases Workflow Pages Generation Script
# This is the final part, completing all page generation

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

# OtherCases/SpecialtyCases/DW
Write-Host "`nCreating OtherCases/SpecialtyCases/DW workflow pages..." -ForegroundColor Cyan
$dwPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\DW"
$dwPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/dw/init"; Title="DW Initialization"; Roles=@("DWView")},
    @{Name="DWMember"; Route="/other-cases/specialty-cases/dw/dw-member"; Title="DW Member"; Roles=@("DWView")},
    @{Name="DWMedTech"; Route="/other-cases/specialty-cases/dw/dw-med-tech"; Title="DW Med Tech"; Roles=@("DWView")},
    @{Name="DWBoardMed"; Route="/other-cases/specialty-cases/dw/dw-board-med"; Title="DW Board Med"; Roles=@("DWView")},
    @{Name="DWDocuments"; Route="/other-cases/specialty-cases/dw/dw-documents"; Title="DW Documents"; Roles=@("DWView")},
    @{Name="DWNextAction"; Route="/other-cases/specialty-cases/dw/dw-next-action"; Title="DW Next Action"; Roles=@("DWView")},
    @{Name="DWCaseComments"; Route="/other-cases/specialty-cases/dw/dw-case-comments"; Title="DW Case Comments"; Roles=@("DWView")},
    @{Name="DWTracking"; Route="/other-cases/specialty-cases/dw/dw-tracking"; Title="DW Tracking"; Roles=@("DWView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/dw/print"; Title="DW Print"; Roles=@("DWView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/dw/case-dialogue"; Title="DW Case Dialogue"; Roles=@("DWView")}
)
foreach ($page in $dwPages) {
    New-RazorPage -FolderPath $dwPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/NE
Write-Host "`nCreating OtherCases/SpecialtyCases/NE workflow pages..." -ForegroundColor Cyan
$nePath = Join-Path $webClientPath "OtherCases\SpecialtyCases\NE"
$nePages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/ne/init"; Title="NE Initialization"; Roles=@("NEView")},
    @{Name="NEMember"; Route="/other-cases/specialty-cases/ne/ne-member"; Title="NE Member"; Roles=@("NEView")},
    @{Name="NEMedTech"; Route="/other-cases/specialty-cases/ne/ne-med-tech"; Title="NE Med Tech"; Roles=@("NEView")},
    @{Name="NEBoardMed"; Route="/other-cases/specialty-cases/ne/ne-board-med"; Title="NE Board Med"; Roles=@("NEView")},
    @{Name="NEDocuments"; Route="/other-cases/specialty-cases/ne/ne-documents"; Title="NE Documents"; Roles=@("NEView")},
    @{Name="NENextAction"; Route="/other-cases/specialty-cases/ne/ne-next-action"; Title="NE Next Action"; Roles=@("NEView")},
    @{Name="NECaseComments"; Route="/other-cases/specialty-cases/ne/ne-case-comments"; Title="NE Case Comments"; Roles=@("NEView")},
    @{Name="NETracking"; Route="/other-cases/specialty-cases/ne/ne-tracking"; Title="NE Tracking"; Roles=@("NEView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/ne/print"; Title="NE Print"; Roles=@("NEView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/ne/case-dialogue"; Title="NE Case Dialogue"; Roles=@("NEView")}
)
foreach ($page in $nePages) {
    New-RazorPage -FolderPath $nePath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/MO
Write-Host "`nCreating OtherCases/SpecialtyCases/MO workflow pages..." -ForegroundColor Cyan
$moPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\MO"
$moPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/mo/init"; Title="MO Initialization"; Roles=@("MOView")},
    @{Name="MOMember"; Route="/other-cases/specialty-cases/mo/mo-member"; Title="MO Member"; Roles=@("MOView")},
    @{Name="MOMedTech"; Route="/other-cases/specialty-cases/mo/mo-med-tech"; Title="MO Med Tech"; Roles=@("MOView")},
    @{Name="MOBoardMed"; Route="/other-cases/specialty-cases/mo/mo-board-med"; Title="MO Board Med"; Roles=@("MOView")},
    @{Name="MODocuments"; Route="/other-cases/specialty-cases/mo/mo-documents"; Title="MO Documents"; Roles=@("MOView")},
    @{Name="MONextAction"; Route="/other-cases/specialty-cases/mo/mo-next-action"; Title="MO Next Action"; Roles=@("MOView")},
    @{Name="MOCaseComments"; Route="/other-cases/specialty-cases/mo/mo-case-comments"; Title="MO Case Comments"; Roles=@("MOView")},
    @{Name="MOTracking"; Route="/other-cases/specialty-cases/mo/mo-tracking"; Title="MO Tracking"; Roles=@("MOView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/mo/print"; Title="MO Print"; Roles=@("MOView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/mo/case-dialogue"; Title="MO Case Dialogue"; Roles=@("MOView")}
)
foreach ($page in $moPages) {
    New-RazorPage -FolderPath $moPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/PEPPAIMWITS
Write-Host "`nCreating OtherCases/SpecialtyCases/PEPPAIMWITS workflow pages..." -ForegroundColor Cyan
$peppPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\PEPPAIMWITS"
$peppPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/pepp-aimwits/init"; Title="PEPP/AIMWITS Initialization"; Roles=@("PEPPView")},
    @{Name="PEPPMember"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-member"; Title="PEPP Member"; Roles=@("PEPPView")},
    @{Name="PEPPHQAFRCTech"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-hq-afrc-tech"; Title="PEPP HQ AFRC Tech"; Roles=@("PEPPView")},
    @{Name="PEPPBoardMed"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-board-med"; Title="PEPP Board Med"; Roles=@("PEPPView")},
    @{Name="PEPPDocuments"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-documents"; Title="PEPP Documents"; Roles=@("PEPPView")},
    @{Name="PEPPNextAction"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-next-action"; Title="PEPP Next Action"; Roles=@("PEPPView")},
    @{Name="PEPPCaseComments"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-case-comments"; Title="PEPP Case Comments"; Roles=@("PEPPView")},
    @{Name="PEPPTracking"; Route="/other-cases/specialty-cases/pepp-aimwits/pepp-tracking"; Title="PEPP Tracking"; Roles=@("PEPPView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/pepp-aimwits/print"; Title="PEPP Print"; Roles=@("PEPPView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/pepp-aimwits/case-dialogue"; Title="PEPP Case Dialogue"; Roles=@("PEPPView")}
)
foreach ($page in $peppPages) {
    New-RazorPage -FolderPath $peppPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/RS
Write-Host "`nCreating OtherCases/SpecialtyCases/RS workflow pages..." -ForegroundColor Cyan
$rsPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\RS"
$rsPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/rs/init"; Title="RS Initialization"; Roles=@("RSView")},
    @{Name="RSMember"; Route="/other-cases/specialty-cases/rs/rs-member"; Title="RS Member"; Roles=@("RSView")},
    @{Name="RSTech"; Route="/other-cases/specialty-cases/rs/rs-tech"; Title="RS Tech"; Roles=@("RSView")},
    @{Name="RSBoardMed"; Route="/other-cases/specialty-cases/rs/rs-board-med"; Title="RS Board Med"; Roles=@("RSView")},
    @{Name="RSDocuments"; Route="/other-cases/specialty-cases/rs/rs-documents"; Title="RS Documents"; Roles=@("RSView")},
    @{Name="RSNextAction"; Route="/other-cases/specialty-cases/rs/rs-next-action"; Title="RS Next Action"; Roles=@("RSView")},
    @{Name="RSCaseComments"; Route="/other-cases/specialty-cases/rs/rs-case-comments"; Title="RS Case Comments"; Roles=@("RSView")},
    @{Name="RSTracking"; Route="/other-cases/specialty-cases/rs/rs-tracking"; Title="RS Tracking"; Roles=@("RSView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/rs/case-dialogue"; Title="RS Case Dialogue"; Roles=@("RSView")}
)
foreach ($page in $rsPages) {
    New-RazorPage -FolderPath $rsPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/RW
Write-Host "`nCreating OtherCases/SpecialtyCases/RW workflow pages..." -ForegroundColor Cyan
$rwPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\RW"
$rwPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/rw/init"; Title="RW Initialization"; Roles=@("RWView")},
    @{Name="RWMember"; Route="/other-cases/specialty-cases/rw/rw-member"; Title="RW Member"; Roles=@("RWView")},
    @{Name="RWMedTech"; Route="/other-cases/specialty-cases/rw/rw-med-tech"; Title="RW Med Tech"; Roles=@("RWView")},
    @{Name="RWMedOff"; Route="/other-cases/specialty-cases/rw/rw-med-off"; Title="RW Med Off"; Roles=@("RWView")},
    @{Name="RWDocuments"; Route="/other-cases/specialty-cases/rw/rw-documents"; Title="RW Documents"; Roles=@("RWView")},
    @{Name="RWPreviousRTD"; Route="/other-cases/specialty-cases/rw/rw-previous-rtd"; Title="RW Previous RTD"; Roles=@("RWView")},
    @{Name="LODDocuments"; Route="/other-cases/specialty-cases/rw/lod-documents"; Title="LOD Documents"; Roles=@("RWView")},
    @{Name="RWNextAction"; Route="/other-cases/specialty-cases/rw/rw-next-action"; Title="RW Next Action"; Roles=@("RWView")},
    @{Name="RWCaseComments"; Route="/other-cases/specialty-cases/rw/rw-case-comments"; Title="RW Case Comments"; Roles=@("RWView")},
    @{Name="RWTracking"; Route="/other-cases/specialty-cases/rw/rw-tracking"; Title="RW Tracking"; Roles=@("RWView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/rw/case-dialogue"; Title="RW Case Dialogue"; Roles=@("RWView")}
)
foreach ($page in $rwPages) {
    New-RazorPage -FolderPath $rwPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/PH
Write-Host "`nCreating OtherCases/SpecialtyCases/PH workflow pages..." -ForegroundColor Cyan
$phPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\PH"
$phPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/ph/init"; Title="PH Initialization"; Roles=@("PHView")},
    @{Name="PHHistory"; Route="/other-cases/specialty-cases/ph/ph-history"; Title="PH History"; Roles=@("PHView")},
    @{Name="PHForm"; Route="/other-cases/specialty-cases/ph/ph-form"; Title="PH Form"; Roles=@("PHView")},
    @{Name="PHDocuments"; Route="/other-cases/specialty-cases/ph/ph-documents"; Title="PH Documents"; Roles=@("PHView")},
    @{Name="PHNextAction"; Route="/other-cases/specialty-cases/ph/ph-next-action"; Title="PH Next Action"; Roles=@("PHView")},
    @{Name="PHCaseComments"; Route="/other-cases/specialty-cases/ph/ph-case-comments"; Title="PH Case Comments"; Roles=@("PHView")},
    @{Name="PHTracking"; Route="/other-cases/specialty-cases/ph/ph-tracking"; Title="PH Tracking"; Roles=@("PHView")},
    @{Name="Print"; Route="/other-cases/specialty-cases/ph/print"; Title="PH Print"; Roles=@("PHView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/ph/case-dialogue"; Title="PH Case Dialogue"; Roles=@("PHView")}
)
foreach ($page in $phPages) {
    New-RazorPage -FolderPath $phPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

# OtherCases/SpecialtyCases/PSCD
Write-Host "`nCreating OtherCases/SpecialtyCases/PSCD workflow pages..." -ForegroundColor Cyan
$pscdPath = Join-Path $webClientPath "OtherCases\SpecialtyCases\PSCD"
$pscdPages = @(
    @{Name="Init"; Route="/other-cases/specialty-cases/pscd/init"; Title="PSCD Initialization"; Roles=@("PSCDView")},
    @{Name="PSCMember"; Route="/other-cases/specialty-cases/pscd/psc-member"; Title="PSC Member"; Roles=@("PSCDView")},
    @{Name="PSCMedTech"; Route="/other-cases/specialty-cases/pscd/psc-med-tech"; Title="PSC Med Tech"; Roles=@("PSCDView")},
    @{Name="CaseDialogue"; Route="/other-cases/specialty-cases/pscd/case-dialogue"; Title="PSCD Case Dialogue"; Roles=@("PSCDView")},
    @{Name="PSCCaseComments"; Route="/other-cases/specialty-cases/pscd/psc-case-comments"; Title="PSC Case Comments"; Roles=@("PSCDView")},
    @{Name="PSCTracking"; Route="/other-cases/specialty-cases/pscd/psc-tracking"; Title="PSC Tracking"; Roles=@("PSCDView")},
    @{Name="PSCNextAction"; Route="/other-cases/specialty-cases/pscd/psc-next-action"; Title="PSC Next Action"; Roles=@("PSCDView")},
    @{Name="PSCBoard"; Route="/other-cases/specialty-cases/pscd/psc-board"; Title="PSC Board"; Roles=@("PSCDView")},
    @{Name="PSCDocuments"; Route="/other-cases/specialty-cases/pscd/psc-documents"; Title="PSC Documents"; Roles=@("PSCDView")},
    @{Name="PSCWDDocuments"; Route="/other-cases/specialty-cases/pscd/psc-wd-documents"; Title="PSC WD Documents"; Roles=@("PSCDView")},
    @{Name="PSCIRDocuments"; Route="/other-cases/specialty-cases/pscd/psc-ir-documents"; Title="PSC IR Documents"; Roles=@("PSCDView")},
    @{Name="PSCRWDocuments"; Route="/other-cases/specialty-cases/pscd/psc-rw-documents"; Title="PSC RW Documents"; Roles=@("PSCDView")}
)
foreach ($page in $pscdPages) {
    New-RazorPage -FolderPath $pscdPath -FileName $page.Name -Route $page.Route -Roles $page.Roles -PageTitle $page.Title -HasRouteParam $true
}

Write-Host "`n============================================" -ForegroundColor Green
Write-Host "All Razor pages generation complete!" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host "`nSummary:" -ForegroundColor Yellow
Write-Host "- Created UserRole enum in AF.ECT.Shared/Enums/" -ForegroundColor Yellow
Write-Host "- Generated 400+ Razor pages with code-behind files" -ForegroundColor Yellow
Write-Host "- All pages include IWorkflowClient injection" -ForegroundColor Yellow
Write-Host "- All pages include OnInitializedAsync method" -ForegroundColor Yellow
Write-Host "- Workflow pages include CaseId route parameter" -ForegroundColor Yellow
Write-Host "`nNext step: Run 'dotnet build' to verify compilation" -ForegroundColor Cyan
