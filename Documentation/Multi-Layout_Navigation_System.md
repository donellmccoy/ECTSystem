# Multi-Layout Navigation System

## Overview

The ECTSystem now uses a **multi-layout approach** with a top menu bar for switching between major functional areas and area-specific sidebars for detailed navigation within each section.

## Architecture

### MainLayout (Default)

**File:** `AF.ECT.WebClient/Layout/MainLayout.razor`

- **Top Header:** Application title with user account and logout buttons
- **Top Menu Bar:** Horizontal navigation between major sections:
  - Home
  - LOD
  - Special Cases
  - Other Cases
  - Reports
  - Administration
  - Help
- **Footer:** Copyright, links, and version info
- **Active Indication:** Currently active section highlighted in blue

### Specialized Layouts

Each major section has its own layout with a **left sidebar** containing hierarchical navigation:

#### 1. LODLayout

**File:** `AF.ECT.WebClient/Layout/LODLayout.razor`

**Sections:**

- My LODs, Consults, Audit, Search
- Reinvestigations (with sub-menu)
- Appeals (with sub-menu)
- Restricted SARC (with sub-menu)
- SARC Appeals (with sub-menu)
- Utilities (forms, inbox, overrides)

**Total Pages:** ~23

#### 2. SpecialCasesLayout

**File:** `AF.ECT.WebClient/Layout/SpecialCasesLayout.razor`

**Sections:**

- My Special Cases, Search, Other Cases
- Case Types (expandable):
  - BMT, BCMR, CMAS, CI, DW, INCAP, IRILO, MEB, MH
  - More: MO, NE, PWaiver, AGR, PEPP, RS, RW, WWD, PSCD, PH

**Total Pages:** ~60+

#### 3. OtherCasesLayout

**File:** `AF.ECT.WebClient/Layout/OtherCasesLayout.razor`

**Sections:**

- WWD, P-Waiver, AGR Certification, MEB
- Specialty Cases (nested):
  - INCAP, BMT Waivers MEPS, Congressionals, BCMR, CMAS
  - More: IRILO, MMSO, MH, DW, NE, MO, PEPP/AIMWITS, RS, RW, PH, PSCD

**Total Pages:** ~160+

#### 4. ReportsLayout

**File:** `AF.ECT.WebClient/Layout/ReportsLayout.razor`

**Sections:**

- Ad Hoc Reporting & Results
- LOD Reports (17 different reports):
  - Case History, Category Count, Disapproved, Disposition
  - Graph, Metrics (Cases & Average Times)
  - Packages, Physician Cancelled, PM reports
  - RWOA, SARC, Statistics, Total Count
- Other Reports:
  - PAL Documents, PH Totals, P-Waivers
  - RS Disposition, RFA reports

**Total Pages:** ~24

#### 5. AdministrationLayout

**File:** `AF.ECT.WebClient/Layout/AdministrationLayout.razor`

**Sections:**

- Create (Add Member, Create Unit/User)
- Emails (Templates, Test, Settings)
- Lookup (8 lookup tools)
- Manage (extensive sub-sections):
  - Groups (Edit, Approval, Permissions)
  - Lookups (9 different lookup managers)
  - Content (HyperLinks, Messages, Units, Users)
  - PH Workflow (6 form management pages)
  - Pages (Manage Pages, Access, Banner)
  - Workflows (Workflow View)
  - Memo (Templates, Mapped Values)
- System Admin (Error logs, testing, rules)
- User Management (Edit, Permissions, Activity)
- Workflow Management (Permissions, Steps, Rules)
- My Account (Edit, Print History)

**Total Pages:** ~70+

#### 6. HelpLayout

**File:** `AF.ECT.WebClient/Layout/HelpLayout.razor`

**Sections:**

- My Account (Select, View)
- Documentation:
  - Policy documents (ALOD Manual, AFI, AFRCI, DODI)
  - Briefings (Medical, Personnel, Financial)
  - Guides (LOD Policy, PHA, RTM)
- User Guides:
  - Application guides (Ad Hoc Reporting, Reports)
  - Role-specific guides (8 different roles)
- Release Notes (18 versions with history)
- Training Materials

**Total Pages:** ~50+

## How It Works

### Route-Based Layout Selection

Blazor automatically selects the appropriate layout based on the route:

1. **MainLayout (Default):** Used for pages at the root level (/, /settings, /edit)

2. **Section Layouts:** Used for pages under specific route prefixes:
   - `/lod/*` → LODLayout
   - `/special-cases/*` → SpecialCasesLayout
   - `/other-cases/*` → OtherCasesLayout
   - `/reports/*` → ReportsLayout
   - `/administration/*` → AdministrationLayout
   - `/help/*` → HelpLayout

### Applying Layouts to Pages

Pages specify which layout to use via the `@layout` directive at the top of the Razor file:

```razor
@layout LODLayout
@page "/lod/my-lods"

<PageTitle>My LODs</PageTitle>
<h1>My LODs</h1>
```

### Current Implementation

**All 460+ pages** are currently using the default `MainLayout`. To switch to area-specific layouts, add the `@layout` directive to each page.

## Implementation Steps

### Option 1: Manual (Quick Test)

Update a few sample pages to use specific layouts:

```razor
@layout LODLayout
@page "/lod/my-lods"
```

### Option 2: Automated (Production)

Create a PowerShell script to update all pages based on their folder location:

```powershell
# Update LOD pages
Get-ChildItem "AF.ECT.WebClient/Pages/LOD" -Filter *.razor -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -notmatch "@layout") {
        $content = "@layout LODLayout`n" + $content
        Set-Content $_.FullName -Value $content -NoNewline
    }
}

# Repeat for each section...
```

### Option 3: Default Layout per Folder

Create `_Imports.razor` files in each folder to set the default layout:

**AF.ECT.WebClient/Pages/LOD/_Imports.razor:**

```razor
@layout LODLayout
```

This automatically applies LODLayout to all pages in the LOD folder and subfolders.

## Navigation Flow

### User Experience

1. **Top Menu Bar** (always visible):
   - Click "LOD" → Navigate to `/lod/my-lods`
   - Click "Reports" → Navigate to `/reports/ad-hoc-reporting`
   - Click "Administration" → Navigate to `/administration/create/add-member`

2. **Sidebar Navigation** (area-specific):
   - When in LOD section: See LOD-specific menu items
   - When in Reports section: See Reports-specific menu items
   - Sidebars are collapsible/expandable for nested items

3. **Active Highlighting**:
   - Top menu shows active section in blue
   - Sidebar shows current page selected

## Benefits

### 1. **Organization**

- Clear separation of concerns
- Each area has its own focused navigation
- Reduces cognitive load (only see relevant options)

### 2. **Performance**

- Smaller navigation trees per area
- Faster rendering (less DOM elements)
- Better mobile experience

### 3. **Maintainability**

- Easy to update area-specific navigation
- Add new sections without affecting existing ones
- Clear file organization matches user flow

### 4. **Scalability**

- Can easily add new major sections
- Sidebar menus can be deeply nested
- Supports future growth (400+ pages handled efficiently)

## File Structure

```
AF.ECT.WebClient/
├── Layout/
│   ├── MainLayout.razor              (Top menu + footer)
│   ├── LODLayout.razor               (LOD sidebar)
│   ├── SpecialCasesLayout.razor      (Special Cases sidebar)
│   ├── OtherCasesLayout.razor        (Other Cases sidebar)
│   ├── ReportsLayout.razor           (Reports sidebar)
│   ├── AdministrationLayout.razor    (Administration sidebar)
│   └── HelpLayout.razor              (Help sidebar)
│
├── Pages/
│   ├── LOD/                          (23 pages → LODLayout)
│   ├── SpecialCases/                 (60+ pages → SpecialCasesLayout)
│   ├── OtherCases/                   (160+ pages → OtherCasesLayout)
│   ├── Reports/                      (24 pages → ReportsLayout)
│   ├── Administration/               (70+ pages → AdministrationLayout)
│   └── Help/                         (50+ pages → HelpLayout)
```

## Menu Structure Examples

### LOD Section (Sidebar)

```
📁 LOD
├── 📄 My LODs
├── 📄 My LOD Consults
├── 📄 SC Pending Consults
├── 📄 Start New LOD
├── 🔍 LOD Search
├── 🔄 Reinvestigations
│   ├── Reinvestigations List
│   ├── RR Search
│   └── Process Completed
├── ⚖️ Appeals
│   ├── Appeals List
│   ├── AP Search
│   └── Process Completed Appeals
└── 🔧 Utilities
    ├── Inbox View
    ├── Override
    └── Generate Forms
```

### Reports Section (Sidebar)

```
📊 Reports
├── 📈 Ad Hoc Reporting
├── 📄 LOD Reports (17)
│   ├── Case History
│   ├── Category Count
│   ├── Metrics - Cases
│   ├── Metrics - Avg Times
│   └── ... (13 more)
└── 📋 Other Reports (6)
    ├── PAL Documents
    ├── PH Totals
    └── ... (4 more)
```

### Administration Section (Sidebar)

```
⚙️ Administration
├── ➕ Create
│   ├── Add Member
│   ├── Create Unit
│   └── Create User
├── 📧 Emails (4 pages)
├── 🔍 Lookup (8 pages)
├── 🛠️ Manage
│   ├── Groups (3 pages)
│   ├── Lookups (9 pages)
│   ├── Content (5 pages)
│   ├── PH Workflow (6 pages)
│   ├── Pages (3 pages)
│   └── Workflows (1 page)
├── 🖥️ System Admin (8 pages)
└── 👤 User Management (4 pages)
```

## Next Steps

### Immediate

1. ✅ Created all layout files with navigation
2. ✅ Updated MainLayout with top menu bar
3. ✅ Verified build succeeds

### To Complete

1. **Apply Layouts:** Add `@layout` directives to all 460+ pages OR create `_Imports.razor` files in each folder
2. **Test Navigation:** Run the app and verify menu navigation works
3. **Refine UI:** Adjust sidebar widths, colors, icons as needed
4. **Add Role-Based Security:** Hide menu items based on user roles (using UserRole enum)
5. **Breadcrumbs:** Add breadcrumb trail to show current location
6. **Responsive Design:** Ensure sidebars collapse on mobile devices

## Usage Example

### For Developers Adding New Pages

**Step 1:** Create the page in the appropriate folder:

```bash
AF.ECT.WebClient/Pages/LOD/NewFeature.razor
```

**Step 2:** Add the layout directive:

```razor
@layout LODLayout
@page "/lod/new-feature"
@attribute [Authorize(Roles = "SysAdmin")]

<PageTitle>New Feature</PageTitle>
<h1>New Feature</h1>
```

**Step 3:** Add menu item to the layout:

```razor
<!-- In LODLayout.razor -->
<RadzenPanelMenuItem Text="New Feature" Icon="new_releases" Path="/lod/new-feature" />
```

**Done!** The page automatically appears in the LOD sidebar.

## Summary

- ✅ **MainLayout:** Top-level navigation bar + footer
- ✅ **6 Specialized Layouts:** Area-specific sidebars
- ✅ **460+ Pages:** Ready to be assigned layouts
- ✅ **Build:** All layouts compile successfully
- ✅ **Navigation:** Hierarchical menus with icons
- ✅ **Organization:** Clear separation by functional area
