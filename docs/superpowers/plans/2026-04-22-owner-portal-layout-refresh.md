# Owner Portal Layout Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restructure the owner web portal into focused routes with clearer navigation, richer owner-specific layouts, and the missing notifications/profile/moderation experiences while keeping the shared emerald design system intact.

**Architecture:** Reuse the existing shared shell, notification, audio, and moderation services wherever possible, and only extend the backend where the current contracts are genuinely too thin: owner identity metadata in auth/session, owner profile CRUD, and loading a single owner POI by id. Build the new owner experience as a vertical slice across shared DTOs, ASP.NET Core endpoints, Blazor services, and bUnit/xUnit coverage so each route lands with tests and does not depend on one giant refactor.

**Tech Stack:** .NET 8, ASP.NET Core controllers, EF Core migrations, Blazor WebAssembly, shared Razor components, bUnit, xUnit

---

## Execution Notes

- Run this plan in a dedicated worktree. The current workspace already contains unrelated edits outside the owner slice. Use `@superpowers:using-git-worktrees` before changing code.
- Use `@superpowers:test-driven-development` for each task.
- Use `@superpowers:verification-before-completion` before claiming the owner refresh is done.
- Approved design spec: `docs/superpowers/specs/2026-04-22-owner-portal-layout-refresh-design.md`

## Scope Check

Keep this as one plan. The route split, shell changes, owner profile backend, and page refreshes are one cohesive owner-portal slice and share the same contracts, layout primitives, and test surfaces.

## File Structure

### Shared contracts and auth/session

- Modify: `src/NarrationApp.Shared/DTOs/Auth/AuthDtos.cs`
  - Add `FullName` to `AuthResponse`.
- Modify: `src/NarrationApp.Shared/DTOs/Owner/OwnerDtos.cs`
  - Add owner profile DTOs and any route-specific owner view models that cannot be expressed cleanly with existing DTOs.
- Modify: `src/NarrationApp.SharedUI/Auth/AuthSession.cs`
  - Persist `FullName` in the client session.
- Modify: `src/NarrationApp.Web/Services/AuthClientService.cs`
  - Map the expanded auth response into session state.

### Server owner data and APIs

- Modify: `src/NarrationApp.Server/Data/Entities/AppUser.cs`
  - Add owner profile metadata such as `Phone`, `ManagedArea`, `LastLoginAtUtc`, and a durable created-at field if one is missing.
- Modify: `src/NarrationApp.Server/Services/AuthService.cs`
  - Stamp `LastLoginAtUtc` on login and include `FullName` in auth responses.
- Modify: `src/NarrationApp.Server/Controllers/OwnerController.cs`
  - Add `GET /api/owner/profile`, `PUT /api/owner/profile`, and `GET /api/owner/pois/{id}`.
- Modify: `src/NarrationApp.Server/Data/Seed/DataSeeder.cs`
  - Seed the new owner fields for baseline users.
- Modify: `tests/NarrationApp.Server.Tests/Support/TestAppDbContextFactory.cs`
  - Populate the new `AppUser` fields in test helpers.
- Create: `src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.cs`
- Create: `src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.Designer.cs`
- Modify: `src/NarrationApp.Server/Data/Migrations/AppDbContextModelSnapshot.cs`

### Web services and registration

- Modify: `src/NarrationApp.Web/Program.cs`
  - Register the new owner profile service.
- Modify: `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
  - Add `GetPoiAsync(int poiId, ...)`.
- Modify: `src/NarrationApp.Web/Services/OwnerPortalService.cs`
  - Call the new owner `GET /api/owner/pois/{id}` endpoint.
- Create: `src/NarrationApp.Web/Services/IOwnerProfileService.cs`
- Create: `src/NarrationApp.Web/Services/OwnerProfileService.cs`
  - Load/update owner profile and bridge password changes through the auth endpoint.

### Shared UI shell and interaction primitives

- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor`
- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor.css`
  - Add optional owner profile card content, richer header slots, and precise nav active-state handling.
- Modify: `src/NarrationApp.SharedUI/Models/ShellNavItem.cs`
  - Support exact vs prefix matching cleanly for `/owner/pois`, `/owner/pois/new`, and detail routes.
- Create: `src/NarrationApp.SharedUI/Components/ConfirmDialog.razor`
- Create: `src/NarrationApp.SharedUI/Components/ConfirmDialog.razor.css`
- Create: `src/NarrationApp.SharedUI/Components/ToastHost.razor`
- Create: `src/NarrationApp.SharedUI/Components/ToastHost.razor.css`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor.css`
- Modify: `src/NarrationApp.Web/Support/RouteHelper.cs`

### Owner pages

- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/Pois.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Pois.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/Moderation.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Moderation.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/Notifications.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Notifications.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/Profile.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Profile.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor.css`
  - Convert the legacy route into a transition wrapper or redirect surface.

### Tests

- Modify: `tests/NarrationApp.Server.Tests/Services/Auth/AuthServiceTests.cs`
- Create: `tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Services/AuthClientServiceTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Services/OwnerProfileServiceTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Components/PortalShellTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Components/NotificationCenterTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Components/ConfirmDialogTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Components/ToastHostTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiManagementTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/NotificationsTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs`

---

### Task 1: Enrich auth/session with owner identity metadata

**Files:**
- Modify: `src/NarrationApp.Shared/DTOs/Auth/AuthDtos.cs`
- Modify: `src/NarrationApp.SharedUI/Auth/AuthSession.cs`
- Modify: `src/NarrationApp.Web/Services/AuthClientService.cs`
- Modify: `src/NarrationApp.Server/Data/Entities/AppUser.cs`
- Modify: `src/NarrationApp.Server/Services/AuthService.cs`
- Modify: `src/NarrationApp.Server/Data/Seed/DataSeeder.cs`
- Modify: `tests/NarrationApp.Server.Tests/Support/TestAppDbContextFactory.cs`
- Modify: `tests/NarrationApp.Server.Tests/Services/Auth/AuthServiceTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Services/AuthClientServiceTests.cs`
- Create: `src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.cs`
- Create: `src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.Designer.cs`
- Modify: `src/NarrationApp.Server/Data/Migrations/AppDbContextModelSnapshot.cs`

- [ ] **Step 1: Write the failing auth/session tests**

Add assertions that:
- `AuthService.LoginAsync` returns `FullName`
- `AuthService.LoginAsync` stamps `LastLoginAtUtc`
- `AuthClientService` maps `FullName` into `AuthSession`

- [ ] **Step 2: Run the focused auth tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthServiceTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthClientServiceTests"
```

Expected:
- FAIL with missing `FullName` members and/or missing `LastLoginAtUtc` persistence assertions

- [ ] **Step 3: Implement the minimal auth/session changes**

Implement:
- new `AppUser` metadata fields
- auth response/session mapping for `FullName`
- login timestamp persistence
- seed/test helper updates
- EF Core migration for the new user columns

- [ ] **Step 4: Re-run the focused auth tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthServiceTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthClientServiceTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the auth/session slice**

Run:
```powershell
git add src/NarrationApp.Shared/DTOs/Auth/AuthDtos.cs src/NarrationApp.SharedUI/Auth/AuthSession.cs src/NarrationApp.Web/Services/AuthClientService.cs src/NarrationApp.Server/Data/Entities/AppUser.cs src/NarrationApp.Server/Services/AuthService.cs src/NarrationApp.Server/Data/Seed/DataSeeder.cs tests/NarrationApp.Server.Tests/Support/TestAppDbContextFactory.cs tests/NarrationApp.Server.Tests/Services/Auth/AuthServiceTests.cs tests/NarrationApp.Web.Tests/Services/AuthClientServiceTests.cs src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.cs src/NarrationApp.Server/Data/Migrations/20260422140000_AddOwnerPortalProfileFields.Designer.cs src/NarrationApp.Server/Data/Migrations/AppDbContextModelSnapshot.cs
git commit -m "feat: enrich owner auth session metadata"
```

### Task 2: Add owner profile API and web service

**Files:**
- Modify: `src/NarrationApp.Shared/DTOs/Owner/OwnerDtos.cs`
- Modify: `src/NarrationApp.Server/Controllers/OwnerController.cs`
- Modify: `src/NarrationApp.Web/Program.cs`
- Create: `src/NarrationApp.Web/Services/IOwnerProfileService.cs`
- Create: `src/NarrationApp.Web/Services/OwnerProfileService.cs`
- Create: `tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Services/OwnerProfileServiceTests.cs`

- [ ] **Step 1: Write the failing owner profile tests**

Add tests for:
- `GET /api/owner/profile` returning editable owner profile data plus activity summary
- `PUT /api/owner/profile` updating `FullName`, `Phone`, `ManagedArea`, and `PreferredLanguage`
- `OwnerProfileService` loading and saving that contract

- [ ] **Step 2: Run the focused owner profile tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerControllerTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerProfileServiceTests"
```

Expected:
- FAIL with missing endpoints, missing DTO members, or missing service registration

- [ ] **Step 3: Implement the minimal owner profile slice**

Implement:
- owner profile DTOs in `OwnerDtos`
- `GET /api/owner/profile`
- `PUT /api/owner/profile`
- web profile service registration and HTTP mapping

- [ ] **Step 4: Re-run the focused owner profile tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerControllerTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerProfileServiceTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the owner profile API slice**

Run:
```powershell
git add src/NarrationApp.Shared/DTOs/Owner/OwnerDtos.cs src/NarrationApp.Server/Controllers/OwnerController.cs src/NarrationApp.Web/Program.cs src/NarrationApp.Web/Services/IOwnerProfileService.cs src/NarrationApp.Web/Services/OwnerProfileService.cs tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs tests/NarrationApp.Web.Tests/Services/OwnerProfileServiceTests.cs
git commit -m "feat: add owner profile api"
```

### Task 3: Extend the shared shell and feedback primitives for owner routes

**Files:**
- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor`
- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor.css`
- Modify: `src/NarrationApp.SharedUI/Models/ShellNavItem.cs`
- Create: `src/NarrationApp.SharedUI/Components/ConfirmDialog.razor`
- Create: `src/NarrationApp.SharedUI/Components/ConfirmDialog.razor.css`
- Create: `src/NarrationApp.SharedUI/Components/ToastHost.razor`
- Create: `src/NarrationApp.SharedUI/Components/ToastHost.razor.css`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor.css`
- Modify: `src/NarrationApp.Web/Support/RouteHelper.cs`
- Modify: `tests/NarrationApp.Web.Tests/Components/PortalShellTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Components/ConfirmDialogTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Components/ToastHostTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs`

- [ ] **Step 1: Write the failing layout and component tests**

Add tests for:
- owner sidebar profile card
- owner nav groups and badges
- correct active-state behavior for `/owner/pois`, `/owner/pois/new`, and `/owner/pois/{id}`
- route copy for `Moderation`, `Notifications`, and `Profile`
- reusable confirm dialog and toast host rendering hooks

- [ ] **Step 2: Run the focused shell/layout tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~PortalShellTests|FullyQualifiedName~MainLayoutTests|FullyQualifiedName~ConfirmDialogTests|FullyQualifiedName~ToastHostTests"
```

Expected:
- FAIL with missing shell slots, route copy, or shared feedback components

- [ ] **Step 3: Implement the shared shell extensions**

Implement:
- owner profile card region in `PortalShell`
- nav badge and active-state handling
- owner route copy in `MainLayout`
- shared `ConfirmDialog` and `ToastHost` components ready for owner pages

- [ ] **Step 4: Re-run the focused shell/layout tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~PortalShellTests|FullyQualifiedName~MainLayoutTests|FullyQualifiedName~ConfirmDialogTests|FullyQualifiedName~ToastHostTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the shell/layout slice**

Run:
```powershell
git add src/NarrationApp.SharedUI/Components/PortalShell.razor src/NarrationApp.SharedUI/Components/PortalShell.razor.css src/NarrationApp.SharedUI/Models/ShellNavItem.cs src/NarrationApp.SharedUI/Components/ConfirmDialog.razor src/NarrationApp.SharedUI/Components/ConfirmDialog.razor.css src/NarrationApp.SharedUI/Components/ToastHost.razor src/NarrationApp.SharedUI/Components/ToastHost.razor.css src/NarrationApp.Web/Layout/MainLayout.razor src/NarrationApp.Web/Layout/MainLayout.razor.css src/NarrationApp.Web/Support/RouteHelper.cs tests/NarrationApp.Web.Tests/Components/PortalShellTests.cs tests/NarrationApp.Web.Tests/Components/ConfirmDialogTests.cs tests/NarrationApp.Web.Tests/Components/ToastHostTests.cs tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs
git commit -m "feat: extend owner shell layout"
```

### Task 4: Refresh the owner dashboard around the new layout

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs`

- [ ] **Step 1: Write the failing dashboard tests**

Add tests for:
- welcome banner with owner name and counts
- spotlight/published POI area
- activity feed
- moderation watch summary
- absence of the old readiness-board-only wording

- [ ] **Step 2: Run the focused dashboard tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.DashboardTests"
```

Expected:
- FAIL with missing welcome banner, activity feed, or updated content structure

- [ ] **Step 3: Implement the dashboard refresh**

Implement:
- banner and KPI layout using existing owner dashboard metrics
- activity feed composed from available owner, moderation, and notification data
- moderation watch CTA structure

- [ ] **Step 4: Re-run the focused dashboard tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.DashboardTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the dashboard slice**

Run:
```powershell
git add src/NarrationApp.Web/Pages/Owner/Dashboard.razor src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css tests/NarrationApp.Web.Tests/Pages/Owner/DashboardTests.cs
git commit -m "feat: refresh owner dashboard layout"
```

### Task 5: Split POI management into list, create, and detail routes

**Files:**
- Modify: `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- Modify: `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- Modify: `src/NarrationApp.Server/Controllers/OwnerController.cs`
- Create: `src/NarrationApp.Web/Pages/Owner/Pois.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Pois.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor`
- Modify: `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor.css`
- Modify: `tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiManagementTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs`

- [ ] **Step 1: Write the failing POI route tests**

Add tests for:
- `/owner/pois` list filtering and search
- `/owner/pois/new` create form sections
- `/owner/pois/{id}` preview card, stats, rejection surface, and audio table
- legacy `/owner/poi-management` redirect or wrapper behavior
- owner `GET /api/owner/pois/{id}` endpoint

- [ ] **Step 2: Run the focused POI route tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerControllerTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.PoiManagementTests|FullyQualifiedName~Pages.Owner.PoisTests|FullyQualifiedName~Pages.Owner.PoiCreateTests|FullyQualifiedName~Pages.Owner.PoiDetailTests"
```

Expected:
- FAIL with missing route files, missing owner POI lookup endpoint, or outdated legacy page assertions

- [ ] **Step 3: Implement the split POI experience**

Implement:
- `GetPoiAsync` in the owner portal service and server controller
- POI list page
- POI create page
- POI detail page composed from owner, audio, geofence, and moderation services
- legacy route redirect or thin wrapper

- [ ] **Step 4: Re-run the focused POI route tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerControllerTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.PoiManagementTests|FullyQualifiedName~Pages.Owner.PoisTests|FullyQualifiedName~Pages.Owner.PoiCreateTests|FullyQualifiedName~Pages.Owner.PoiDetailTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the POI route split**

Run:
```powershell
git add src/NarrationApp.Web/Services/IOwnerPortalService.cs src/NarrationApp.Web/Services/OwnerPortalService.cs src/NarrationApp.Server/Controllers/OwnerController.cs src/NarrationApp.Web/Pages/Owner/Pois.razor src/NarrationApp.Web/Pages/Owner/Pois.razor.css src/NarrationApp.Web/Pages/Owner/PoiCreate.razor src/NarrationApp.Web/Pages/Owner/PoiCreate.razor.css src/NarrationApp.Web/Pages/Owner/PoiDetail.razor src/NarrationApp.Web/Pages/Owner/PoiDetail.razor.css src/NarrationApp.Web/Pages/Owner/PoiManagement.razor src/NarrationApp.Web/Pages/Owner/PoiManagement.razor.css tests/NarrationApp.Server.Tests/Controllers/OwnerControllerTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiManagementTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoisTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiCreateTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/PoiDetailTests.cs
git commit -m "feat: split owner poi routes"
```

### Task 6: Add the owner moderation workspace

**Files:**
- Create: `src/NarrationApp.Web/Pages/Owner/Moderation.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Moderation.razor.css`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs`

- [ ] **Step 1: Write the failing moderation page tests**

Add tests for:
- visual stepper text and state
- pending moderation list
- history table with review note visibility
- rejection-focused CTA wording

- [ ] **Step 2: Run the focused moderation tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.ModerationTests"
```

Expected:
- FAIL with missing route/page and missing moderation workspace structure

- [ ] **Step 3: Implement the moderation workspace**

Implement:
- owner moderation page using `IModerationPortalService.GetMineAsync`
- stepper, pending list, and history table derived from existing moderation data
- rejection note rendering aligned with the new POI detail page

- [ ] **Step 4: Re-run the focused moderation tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~Pages.Owner.ModerationTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the moderation workspace**

Run:
```powershell
git add src/NarrationApp.Web/Pages/Owner/Moderation.razor src/NarrationApp.Web/Pages/Owner/Moderation.razor.css tests/NarrationApp.Web.Tests/Pages/Owner/ModerationTests.cs
git commit -m "feat: add owner moderation workspace"
```

### Task 7: Add the owner notifications page

**Files:**
- Create: `src/NarrationApp.Web/Pages/Owner/Notifications.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Notifications.razor.css`
- Modify: `tests/NarrationApp.Web.Tests/Components/NotificationCenterTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/NotificationsTests.cs`

- [ ] **Step 1: Write the failing notifications tests**

Add tests for:
- owner notifications page rendering unread counts and items
- per-item mark read behavior
- mark-all-read behavior
- popover still behaving as a shortcut, not the only notification surface

- [ ] **Step 2: Run the focused notifications tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~NotificationCenterTests|FullyQualifiedName~Pages.Owner.NotificationsTests"
```

Expected:
- FAIL with missing page route and missing owner page actions

- [ ] **Step 3: Implement the notifications workspace**

Implement:
- owner notifications page using `INotificationCenterService`
- list filters and mark-read actions
- optional popover copy tweaks if the old tests need updated expectations

- [ ] **Step 4: Re-run the focused notifications tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~NotificationCenterTests|FullyQualifiedName~Pages.Owner.NotificationsTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the notifications workspace**

Run:
```powershell
git add src/NarrationApp.Web/Pages/Owner/Notifications.razor src/NarrationApp.Web/Pages/Owner/Notifications.razor.css tests/NarrationApp.Web.Tests/Components/NotificationCenterTests.cs tests/NarrationApp.Web.Tests/Pages/Owner/NotificationsTests.cs
git commit -m "feat: add owner notifications page"
```

### Task 8: Add the owner profile page and password flow

**Files:**
- Create: `src/NarrationApp.Web/Pages/Owner/Profile.razor`
- Create: `src/NarrationApp.Web/Pages/Owner/Profile.razor.css`
- Modify: `src/NarrationApp.Web/Services/IOwnerProfileService.cs`
- Modify: `src/NarrationApp.Web/Services/OwnerProfileService.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Services/OwnerProfileServiceTests.cs`

- [ ] **Step 1: Write the failing profile page tests**

Add tests for:
- profile form rendering current owner values
- activity summary rendering
- update profile submission
- password validation and change-password submission

- [ ] **Step 2: Run the focused profile tests and confirm they fail**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerProfileServiceTests|FullyQualifiedName~Pages.Owner.ProfileTests"
```

Expected:
- FAIL with missing page route, missing service methods, or missing password-flow handling

- [ ] **Step 3: Implement the profile page**

Implement:
- profile form bound to `IOwnerProfileService`
- activity summary block
- password change form wired through the service/auth endpoint
- success and error feedback through the shared toast/confirm primitives where appropriate

- [ ] **Step 4: Re-run the focused profile tests and confirm they pass**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~OwnerProfileServiceTests|FullyQualifiedName~Pages.Owner.ProfileTests"
```

Expected:
- PASS

- [ ] **Step 5: Commit the profile workspace**

Run:
```powershell
git add src/NarrationApp.Web/Pages/Owner/Profile.razor src/NarrationApp.Web/Pages/Owner/Profile.razor.css src/NarrationApp.Web/Services/IOwnerProfileService.cs src/NarrationApp.Web/Services/OwnerProfileService.cs tests/NarrationApp.Web.Tests/Pages/Owner/ProfileTests.cs tests/NarrationApp.Web.Tests/Services/OwnerProfileServiceTests.cs
git commit -m "feat: add owner profile page"
```

### Task 9: Verify the whole owner slice and clean up loose ends

**Files:**
- Modify only as needed from previous tasks

- [ ] **Step 1: Run the focused owner regression suite**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore --filter "FullyQualifiedName~AuthServiceTests|FullyQualifiedName~OwnerControllerTests"
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MainLayoutTests|FullyQualifiedName~PortalShellTests|FullyQualifiedName~NotificationCenterTests|FullyQualifiedName~Owner"
```

Expected:
- PASS

- [ ] **Step 2: Run the full server and web test suites**

Run:
```powershell
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Server.Tests\NarrationApp.Server.Tests.csproj --no-restore
dotnet test D:\VinhKhanhFoodStreet\tests\NarrationApp.Web.Tests\NarrationApp.Web.Tests.csproj --no-restore
```

Expected:
- PASS

- [ ] **Step 3: Fix regressions until both suites pass**

Only modify files from previous tasks or the specific regressions uncovered by the suite.

- [ ] **Step 4: Summarize assumptions and deferred follow-ups**

Capture anything intentionally deferred, especially:
- whether image upload stays URL-driven in this iteration
- whether any owner page still deserves a dedicated aggregate endpoint later

- [ ] **Step 5: Commit the verification cleanup**

Run:
```powershell
git add -A
git commit -m "test: verify owner portal refresh"
```
