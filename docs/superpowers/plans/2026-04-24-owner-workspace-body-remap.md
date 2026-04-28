# Owner Workspace Body Remap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remap the six owner page bodies to the approved dark workspace UI while keeping them wired to real backend data, admin moderation/audio flows, and one-file representative image upload.

**Architecture:** Add owner-focused workspace DTOs/endpoints where the current owner contracts are too thin, then remap each owner page body onto a shared visual language built from reusable owner body classes and focused page-specific structures. Keep the existing shell/sidebar intact, keep `Poi.ImageUrl` as the single persisted representative image field, and route owner actions through the existing POI, audio, moderation, and profile services wherever possible.

**Tech Stack:** ASP.NET Core 8 controllers/services, EF Core, Blazor Web App Razor components, CSS isolation plus shared global owner body classes, bUnit, xUnit

---

## File Structure

**Create:**
- `docs/superpowers/plans/2026-04-24-owner-workspace-body-remap.md`
- `src/NarrationApp.Shared/DTOs/Owner/OwnerWorkspaceDtos.cs`
- `src/NarrationApp.Web/Pages/Owner/OwnerWorkspace.razor.css` or shared owner body CSS placed in `src/NarrationApp.Web/wwwroot/css/app.css`

**Modify:**
- `src/NarrationApp.Server/Controllers/OwnerController.cs`
- `src/NarrationApp.Server/Controllers/PoisController.cs`
- `src/NarrationApp.Server/Services/PoiService.cs`
- `src/NarrationApp.Server/Services/IStorageService.cs` consumers if image upload support needs shared helper usage
- `src/NarrationApp.Shared/DTOs/Poi/PoiDtos.cs`
- `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- `src/NarrationApp.Web/Pages/Owner/Dashboard.razor`
- `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- `src/NarrationApp.Web/Pages/Owner/Pois.razor`
- `src/NarrationApp.Web/Pages/Owner/Pois.razor.css`
- `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor`
- `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css`
- `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor`
- `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css`
- `src/NarrationApp.Web/Pages/Owner/Moderation.razor`
- `src/NarrationApp.Web/Pages/Owner/Moderation.razor.css`
- `src/NarrationApp.Web/Pages/Owner/Profile.razor`
- `src/NarrationApp.Web/Pages/Owner/Profile.razor.css`

**Test:**
- `tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs`
- `tests/NarrationApp.Server.Tests/Controllers/PoisControllerTests.cs` if image endpoints are exposed there
- `tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs`
- `tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs`
- `tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs`
- `tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs`
- `tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs`
- `tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs`

---

### Task 1: Define Owner Workspace Contracts

**Files:**
- Create: `src/NarrationApp.Shared/DTOs/Owner/OwnerWorkspaceDtos.cs`
- Modify: `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- Modify: `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs`

- [ ] **Step 1: Write failing web tests for new owner workspace surfaces**

Add focused failing assertions for:
- dashboard rendering stat-card row + published table + recent activity panel
- POI list rendering stat-card row + table headers such as `NỘI DUNG NGUỒN`
- moderation rendering stat-card row + pending/history table structure

- [ ] **Step 2: Run focused tests to verify they fail**

Run:
```powershell
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~DashboardTests|FullyQualifiedName~PoisTests|FullyQualifiedName~ModerationTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because workspace DTO/service methods and new markup do not exist yet.

- [ ] **Step 3: Add shared DTOs and service interface methods**

Define owner workspace DTOs for:
- dashboard workspace
- POI list workspace
- moderation workspace
- POI detail workspace
- image upload/remove interactions if a custom DTO is required

Add matching methods on `IOwnerPortalService` and `OwnerPortalService`.

- [ ] **Step 4: Run the same focused tests**

Run the same command as Step 2.

Expected: tests still fail, but now on missing page implementation rather than missing DTO/service contracts.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Shared/DTOs/Owner/OwnerWorkspaceDtos.cs src/NarrationApp.Web/Services/IOwnerPortalService.cs src/NarrationApp.Web/Services/OwnerPortalService.cs tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs
git commit -m "feat: add owner workspace contracts"
```

---

### Task 2: Add Backend Workspace Endpoints

**Files:**
- Modify: `src/NarrationApp.Server/Controllers/OwnerController.cs`
- Test: `tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs`

- [ ] **Step 1: Write failing server tests for owner workspace endpoints**

Cover:
- `GET /api/owner/dashboard/workspace`
- `GET /api/owner/pois/workspace`
- `GET /api/owner/moderation/workspace`
- `GET /api/owner/pois/{id}/workspace`

Assert that results include:
- real owner counts
- published POI rows
- moderation history rows joined with POI names
- detail metrics including QR scans and listen duration

- [ ] **Step 2: Run focused owner controller tests to verify they fail**

Run:
```powershell
dotnet test tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~OwnerControllerTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because endpoints and DTO population do not exist yet.

- [ ] **Step 3: Implement minimal owner workspace endpoints**

In `OwnerController`:
- add workspace endpoints
- compose counts from POIs, moderation requests, notifications, audio assets, visit events
- derive dashboard trend bars from latest 7 calendar days
- resolve moderation/display rows with POI names
- derive `SourceContentKind` for list rows from source script and Vietnamese audio presence
- derive detail metrics for QR scans and total listen duration from `VisitEvents`

- [ ] **Step 4: Re-run focused owner controller tests**

Run the same command as Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Server/Controllers/OwnerController.cs tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs
git commit -m "feat: add owner workspace endpoints"
```

---

### Task 3: Add Representative Image Upload From File

**Files:**
- Modify: `src/NarrationApp.Server/Controllers/PoisController.cs`
- Modify: `src/NarrationApp.Server/Services/PoiService.cs`
- Modify: `src/NarrationApp.Shared/DTOs/Poi/PoiDtos.cs`
- Modify: `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- Modify: `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- Test: `tests/NarrationApp.Server.Tests/Controllers/PoisControllerTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs`

- [ ] **Step 1: Write failing tests for image upload/remove**

Server:
- owner uploads image for owned POI
- unauthorized owner is blocked
- replacing an image updates `Poi.ImageUrl`
- deleting image clears `Poi.ImageUrl`

Web:
- create page accepts one selected file and uploads after POI creation
- detail page uploads a replacement image and updates preview

- [ ] **Step 2: Run focused server and web tests to verify failure**

Run:
```powershell
dotnet test tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~PoisControllerTests" --logger "console;verbosity=minimal"
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~PoiCreateTests|FullyQualifiedName~PoiDetailTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because POI image endpoints and page flow are missing.

- [ ] **Step 3: Implement minimal image upload contract**

Server:
- add `POST /api/pois/{id}/image`
- add `DELETE /api/pois/{id}/image`
- validate type/size
- store via `IStorageService`
- update `Poi.ImageUrl`

Web:
- add multipart upload/delete methods on owner portal service
- add one-file selector model to create/detail pages
- keep `Poi.ImageUrl` as the read/display field

- [ ] **Step 4: Re-run focused tests**

Run the same commands as Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Server/Controllers/PoisController.cs src/NarrationApp.Server/Services/PoiService.cs src/NarrationApp.Shared/DTOs/Poi/PoiDtos.cs src/NarrationApp.Web/Services/IOwnerPortalService.cs src/NarrationApp.Web/Services/OwnerPortalService.cs tests/NarrationApp.Server.Tests/Controllers/PoisControllerTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs
git commit -m "feat: add owner poi image upload"
```

---

### Task 4: Build Shared Owner Body Styling

**Files:**
- Create: shared owner body CSS in `src/NarrationApp.Web/wwwroot/css/app.css` or a new shared owner stylesheet
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/Pois.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/Moderation.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/Profile.razor.css`
- Test: no separate test file; verified through page tests

- [ ] **Step 1: Introduce failing page assertions that depend on shared body class names**

Add assertions for:
- stat strip class
- owner data panel class
- table header labels
- form panel structure

- [ ] **Step 2: Run focused page tests to verify they fail**

Run:
```powershell
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~DashboardTests|FullyQualifiedName~PoisTests|FullyQualifiedName~PoiCreateTests|FullyQualifiedName~PoiDetailTests|FullyQualifiedName~ModerationTests|FullyQualifiedName~ProfileTests" --logger "console;verbosity=minimal"
```

Expected: FAIL on missing shared layout structure.

- [ ] **Step 3: Add shared owner body classes**

Implement reusable visual classes for:
- page title/breadcrumb block
- stat cards
- dark data panels
- owner tables
- owner form panels
- action bars

- [ ] **Step 4: Re-run focused page tests**

Run the same command as Step 2.

Expected: still partially failing until page markup is remapped, but shared class foundation now exists.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Web/wwwroot/css/app.css src/NarrationApp.Web/Pages/Owner/*.razor.css
git commit -m "style: add shared owner workspace body system"
```

---

### Task 5: Remap Dashboard And POI List

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/Pois.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Pois.razor.css`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs`

- [ ] **Step 1: Expand failing dashboard/list tests to the approved body structure**

Dashboard:
- title + breadcrumb
- four stat cards
- published POI table
- recent activity panel

POI list:
- title + breadcrumb
- four stat cards
- POI table headers/rows
- create CTA and rejected row action wording

- [ ] **Step 2: Run focused tests to verify failure**

Run:
```powershell
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~DashboardTests|FullyQualifiedName~PoisTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because current markup is still the old body structure.

- [ ] **Step 3: Implement dashboard/list remap against workspace endpoints**

Dashboard:
- switch to workspace data
- remove bright hero
- render stat strip + published table + recent activity panel

POI list:
- switch to workspace data
- render stat strip + toolbar + table

- [ ] **Step 4: Re-run focused tests**

Run the same command as Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Web/Pages/Owner/Dashboard.razor src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css src/NarrationApp.Web/Pages/Owner/Pois.razor src/NarrationApp.Web/Pages/Owner/Pois.razor.css tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs
git commit -m "feat: remap owner dashboard and poi list"
```

---

### Task 6: Remap Create And Detail

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs`

- [ ] **Step 1: Write failing tests for the new create/detail layout and flows**

Create:
- title + breadcrumb
- left/right workspace columns
- image file chooser
- draft + submit actions

Detail:
- summary hero
- source content panel
- multilingual audio matrix
- lower stat cards
- reject alert panel

- [ ] **Step 2: Run focused tests to verify failure**

Run:
```powershell
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~PoiCreateTests|FullyQualifiedName~PoiDetailTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because the pages still render the old form layout and no file-based image flow.

- [ ] **Step 3: Implement create/detail remap**

Create:
- map fields into two-column workspace
- support file-selected representative image
- implement `Lưu nháp` and `Gửi duyệt`

Detail:
- load workspace DTO or extended detail data
- render summary hero, audio matrix, metrics strip
- support image replacement/remove and moderation resend

- [ ] **Step 4: Re-run focused tests**

Run the same command as Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Web/Pages/Owner/PoiCreate.razor src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css src/NarrationApp.Web/Pages/Owner/PoiDetail.razor src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs
git commit -m "feat: remap owner create and detail views"
```

---

### Task 7: Remap Moderation And Profile

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Owner/Moderation.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Moderation.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/Profile.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Profile.razor.css`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs`

- [ ] **Step 1: Write failing tests for approved moderation/profile body layout**

Moderation:
- title + breadcrumb
- stat cards
- flow strip
- pending/history data panels

Profile:
- title + breadcrumb
- account form panel
- password panel
- activity summary panel

- [ ] **Step 2: Run focused tests to verify failure**

Run:
```powershell
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~ModerationTests|FullyQualifiedName~ProfileTests" --logger "console;verbosity=minimal"
```

Expected: FAIL because the old hero-based layouts are still in place.

- [ ] **Step 3: Implement moderation/profile remap**

Moderation:
- switch to workspace data
- render stat strip, process flow, pending and history tables

Profile:
- replace hero + metric grid with two-column account/password/activity layout
- preserve auth-session refresh after profile save

- [ ] **Step 4: Re-run focused tests**

Run the same command as Step 2.

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/NarrationApp.Web/Pages/Owner/Moderation.razor src/NarrationApp.Web/Pages/Owner/Moderation.razor.css src/NarrationApp.Web/Pages/Owner/Profile.razor src/NarrationApp.Web/Pages/Owner/Profile.razor.css tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs
git commit -m "feat: remap owner moderation and profile"
```

---

### Task 8: Full Verification And Wrap-Up

**Files:**
- Modify only if verification reveals defects

- [ ] **Step 1: Run focused owner regression**

Run:
```powershell
dotnet test tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~OwnerControllerTests|FullyQualifiedName~PoisControllerTests" --logger "console;verbosity=minimal"
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --filter "FullyQualifiedName~MainLayoutTests|FullyQualifiedName~DashboardTests|FullyQualifiedName~PoisTests|FullyQualifiedName~PoiCreateTests|FullyQualifiedName~PoiDetailTests|FullyQualifiedName~ModerationTests|FullyQualifiedName~ProfileTests" --logger "console;verbosity=minimal"
```

Expected: PASS.

- [ ] **Step 2: Run full server and web suites**

Run:
```powershell
dotnet test tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore -m:1 --logger "console;verbosity=minimal"
dotnet test tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore -m:1 --logger "console;verbosity=minimal"
```

Expected: PASS.

- [ ] **Step 3: Fix any verification regressions**

If any failures appear, return to TDD for that slice before continuing.

- [ ] **Step 4: Summarize the delivered owner workspace system**

Include:
- body remap across six owner pages
- backend workspace endpoints
- admin-linked flows
- representative image upload
- verification evidence

- [ ] **Step 5: Commit final verification fixes if needed**

```bash
git add <files fixed during verification>
git commit -m "fix: close owner workspace remap regressions"
```

