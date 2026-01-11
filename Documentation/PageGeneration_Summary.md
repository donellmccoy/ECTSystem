# Page Generation Summary

## Overview

Successfully generated 400+ Razor pages with code-behind files for the ECTSystem application based on the site_access.json navigation structure.

## What Was Created

### 1. UserRole Enum

**File:** `AF.ECT.Shared/Enums/UserRole.cs`

- Contains 100+ role definitions mapped from site_access.json
- Examples: SysAdmin, LodSearch, MyLod, LodView, APView, RSARCView, BMTView, MEBView, etc.
- Used in all `[Authorize(Roles = "...")]` attributes throughout the application

### 2. PowerShell Generation Scripts

Created 5 scripts in `Scripts/` directory to automate page generation:

#### Generate-RazorPages.ps1

- Common pages (Default, DevLogin, About, SessionViewer, etc.)
- LOD workflow pages (MyLODs, LODSearch, StartNewLOD, etc.)
- Special Cases pages (MySpecialCases, Search, OtherCases)
- Case type pages (BMT, BCMR, CMAS, CI, DW, INCAP, IRILO, MEB, MH, MO, NE, PWaiver, AGR, PEPP, RS, RW, WWD, PSCD, PH)
- **Total:** ~100 pages

#### Generate-RazorPages-Part2.ps1

- Reports pages (AdHocReporting, LOD reports, PH reports, etc.)
- Administration structure:
  - Create (AddMember, CreateUnit, CreateUser)
  - Emails (EmailTemplates, EmailTest, InactiveEmailSettings, SendSystemEmails)
  - Lookup (ARCNetUserLookup, ApprovalAuthorities, CaseLocks, etc.)
  - Manage subfolders (Groups, PHWorkflowForm, Pages, Workflows, Memo)
  - SystemAdmin (Rules, StatusCodes, ErrorLog, etc.)
- **Total:** ~75 pages

#### Generate-RazorPages-Part3.ps1

- Help pages (SelectAccount, MyAccount)
- Documentation (ALODManual, MasterLODChecklist, policy documents)
- ReleaseNotes (18 version-specific pages)
- Root pages (Edit, SearchUnit, UnitAdministration)
- Main workflow pages:
  - LineOfDuty (17 pages: Init, Member, UnitCC, Medical, etc.)
  - ReinvestigationRequest (12 pages)
  - Appeal (11 pages)
  - RestrictedSARC (12 pages)
  - SARCAppeal (11 pages)
- **Total:** ~95 pages

#### Generate-RazorPages-Part4.ps1

- OtherCases workflows:
  - WWD (11 pages)
  - PWaiver (10 pages)
  - AGRCert (9 pages)
  - MEB (11 pages)
- SpecialtyCases workflows:
  - INCAP (11 pages)
  - BMTWaiversMEPS (11 pages)
  - Congressionals (10 pages)
  - BCMR (10 pages)
  - CMAS (10 pages)
  - IRILO (12 pages)
  - MMSO (9 pages)
  - MH (10 pages)
- **Total:** ~110 pages

#### Generate-RazorPages-Part5.ps1

- Remaining SpecialtyCases workflows:
  - DW (10 pages)
  - NE (10 pages)
  - MO (10 pages)
  - PEPPAIMWITS (10 pages)
  - RS (9 pages)
  - RW (11 pages)
  - PH (9 pages)
  - PSCD (12 pages)
- **Total:** ~80 pages

## Page Structure

### Razor Page (.razor)

Each page includes:

- `@page` directive with route (e.g., `@page "/lod/my-lods"`)
- `@attribute [Authorize(Roles = "...")]` for role-based access
- `@using Microsoft.AspNetCore.Authorization`
- `<PageTitle>` element
- Basic HTML structure with "This page is under development" message

### Code-Behind File (.razor.cs)

Each code-behind includes:

- Proper namespace matching folder structure
- XML documentation comments for IntelliSense
- `IWorkflowClient` injection via `[Inject]` attribute
- `CaseId` parameter for workflow pages (e.g., `/lod/member/{caseId:int}`)
- `OnInitializedAsync()` method for component initialization

## Example Code

### Razor Page

```razor
@page "/lod/my-lods"
@attribute [Authorize(Roles = "MyLod")]
@using Microsoft.AspNetCore.Authorization

<PageTitle>My LODs</PageTitle>

<h1>My LODs</h1>

<p>This page is under development.</p>
```

### Code-Behind

```csharp
using Microsoft.AspNetCore.Components;

namespace AF.ECT.WebClient.Pages.LOD;

/// <summary>
/// Code-behind for the MyLODs page.
/// </summary>
public partial class MyLODs
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
```

## Issues Resolved

### Namespace Conflict

**Problem:** Created a folder named "System" under Administration, which conflicted with .NET's System namespace.

**Solution:**

1. Renamed folder from `Administration/System` to `Administration/SystemAdmin`
2. Updated all namespaces in code-behind files from `AF.ECT.WebClient.Pages.Administration.System` to `AF.ECT.WebClient.Pages.Administration.SystemAdmin`
3. Updated all routes from `/administration/system/` to `/administration/system-admin/`

## Folder Structure

```
AF.ECT.WebClient/Pages/
├── Common/                     (25 pages)
├── LOD/                        (23 pages)
├── SpecialCases/              (3 pages)
│   ├── BMT/                   (3 pages)
│   ├── BCMR/                  (3 pages)
│   ├── CMAS/                  (3 pages)
│   ├── CI/                    (3 pages)
│   ├── DW/                    (3 pages)
│   ├── INCAP/                 (3 pages)
│   ├── IRILO/                 (3 pages)
│   ├── MEB/                   (3 pages)
│   ├── MH/                    (3 pages)
│   ├── MO/                    (3 pages)
│   ├── NE/                    (3 pages)
│   ├── PWaiver/               (3 pages)
│   ├── AGRCert/               (3 pages)
│   ├── PEPPAIMWITS/           (3 pages)
│   ├── RS/                    (3 pages)
│   ├── RW/                    (3 pages)
│   ├── WWD/                   (3 pages)
│   ├── PSCD/                  (3 pages)
│   └── PH/                    (4 pages)
├── Reports/                   (24 pages)
├── Administration/            (41 pages)
│   ├── Create/               (3 pages)
│   ├── Emails/               (4 pages)
│   ├── Lookup/               (8 pages)
│   ├── Manage/               (14 pages)
│   │   ├── Groups/           (2 pages)
│   │   ├── PHWorkflowForm/   (5 pages)
│   │   ├── Pages/            (2 pages)
│   │   ├── Workflows/        (4 pages)
│   │   └── Memo/             (2 pages)
│   └── SystemAdmin/          (12 pages)
│       └── Error/            (4 pages)
├── Help/                      (2 pages)
├── Documentation/             (33 pages)
│   └── ReleaseNotes/         (18 pages)
├── Root/                      (3 pages)
├── LineOfDuty/               (17 pages)
├── ReinvestigationRequest/   (12 pages)
├── Appeal/                   (11 pages)
├── RestrictedSARC/           (12 pages)
├── SARCAppeal/               (11 pages)
└── OtherCases/               (160+ pages)
    ├── WWD/                  (11 pages)
    ├── PWaiver/              (10 pages)
    ├── AGRCert/              (9 pages)
    ├── MEB/                  (11 pages)
    └── SpecialtyCases/       (120+ pages)
        ├── INCAP/            (11 pages)
        ├── BMTWaiversMEPS/   (11 pages)
        ├── Congressionals/   (10 pages)
        ├── BCMR/             (10 pages)
        ├── CMAS/             (10 pages)
        ├── IRILO/            (12 pages)
        ├── MMSO/             (9 pages)
        ├── MH/               (10 pages)
        ├── DW/               (10 pages)
        ├── NE/               (10 pages)
        ├── MO/               (10 pages)
        ├── PEPPAIMWITS/      (10 pages)
        ├── RS/               (9 pages)
        ├── RW/               (11 pages)
        ├── PH/               (9 pages)
        └── PSCD/             (12 pages)
```

## Verification

### Build Status

✅ **SUCCESS** - Solution builds without errors after resolving System namespace conflict.

```
Build succeeded in 6.8s
AF.ECT.WebClient succeeded → AF.ECT.WebClient\bin\Debug\net9.0\wwwroot
```

### Statistics

- **Total Pages Generated:** 460+
- **Total Code-Behind Files:** 460+
- **Total Files Created:** 920+
- **Roles Defined:** 100+
- **Folder Structure Depth:** Up to 5 levels deep

## Next Steps

### 1. Implement Page Functionality

Each page currently displays "This page is under development." Developers should:

- Add Blazor components for UI
- Implement gRPC calls using injected `IWorkflowClient`
- Add data binding and form validation
- Implement business logic in `OnInitializedAsync()` or other lifecycle methods

### 2. Update Navigation

Update `nav_tree.json` and navigation components to reflect the new page structure.

### 3. Add Route Guards

Consider adding additional authorization checks in code-behind files for fine-grained access control.

### 4. Testing

- Unit test each page component
- Integration test workflow navigation
- Security test role-based authorization

### 5. Documentation

- Add user guides for each workflow
- Document role permissions and access patterns
- Create API documentation for gRPC calls

## Files Modified

### Created Files

1. `AF.ECT.Shared/Enums/UserRole.cs`
2. `Scripts/Generate-RazorPages.ps1`
3. `Scripts/Generate-RazorPages-Part2.ps1`
4. `Scripts/Generate-RazorPages-Part3.ps1`
5. `Scripts/Generate-RazorPages-Part4.ps1`
6. `Scripts/Generate-RazorPages-Part5.ps1`
7. 920+ Razor page and code-behind files in `AF.ECT.WebClient/Pages/`

### Modified Files

None - all existing files remain unchanged.

## Lessons Learned

1. **Namespace Conflicts:** Avoid using .NET reserved names (System, Microsoft, etc.) for folder names in ASP.NET projects.
2. **Automation:** PowerShell scripts are highly effective for bulk file generation with consistent patterns.
3. **File-Scoped Namespaces:** Using file-scoped namespaces (`namespace X;`) reduces indentation and improves readability.
4. **XML Documentation:** Adding XML comments to all code-behind members improves IntelliSense and maintainability.
5. **Partial Classes:** Separating markup (.razor) from logic (.razor.cs) follows best practices and improves code organization.

## References

- Source JSON: `AF.ECT.WebClient/site_access.json`
- Coding Instructions: `.github/copilot-instructions.md`
- Project Documentation: `Documentation/`
