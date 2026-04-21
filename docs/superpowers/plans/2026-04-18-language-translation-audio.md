# Language, Translation, and Audio Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align the admin web and server pipeline so Vietnamese source text drives translation, saved translations drive audio generation, and admin can manage system languages from a dedicated workspace.

**Architecture:** Add a managed-language catalog, enrich translation and audio records with workflow metadata, and move audio generation for non-Vietnamese languages behind a server-side "generate from saved translation" path. Update the admin UI so `Quản lý ngôn ngữ`, `Bản dịch`, and `Audio` each own one clear responsibility.

**Tech Stack:** .NET 8, ASP.NET Core controllers, Blazor WebAssembly, EF Core, bUnit, xUnit

---

### Task 1: Write the spec-backed failing tests for managed languages

**Files:**
- Create: `tests/NarrationApp.Server.Tests/Services/Languages/ManagedLanguageServiceTests.cs`
- Create: `tests/NarrationApp.Web.Tests/Pages/Admin/LanguageManagementTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs`

- [ ] Step 1: Write failing tests for language catalog service and admin page
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement minimal managed-language entity, DTOs, service, controller, portal service, and page
- [ ] Step 4: Run focused tests and confirm they pass

### Task 2: Write the failing tests for translation workflow metadata

**Files:**
- Modify: `tests/NarrationApp.Server.Tests/Services/Translations/TranslationServiceTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Admin/TranslationReviewTests.cs`
- Modify: `src/NarrationApp.Shared/DTOs/Translation/TranslationDtos.cs`
- Modify: `src/NarrationApp.Server/Data/Entities/PoiTranslation.cs`

- [ ] Step 1: Add tests for `Source / AutoTranslated / Reviewed` behavior
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement translation status fields and save rules
- [ ] Step 4: Run focused tests and confirm they pass

### Task 3: Write the failing tests for audio generation from saved translations

**Files:**
- Modify: `tests/NarrationApp.Server.Tests/Services/Audio/AudioServiceTests.cs`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Admin/AudioManagementTests.cs`
- Modify: `src/NarrationApp.Shared/DTOs/Audio/AudioDtos.cs`
- Modify: `src/NarrationApp.Server/Data/Entities/AudioAsset.cs`

- [ ] Step 1: Add tests proving non-VI audio generation requires saved translation
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement DTO/entity changes, controller endpoint, and portal service method
- [ ] Step 4: Run focused tests and confirm they pass

### Task 4: Update admin navigation and route copy

**Files:**
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor`
- Modify: `src/NarrationApp.Web/Support/RouteHelper.cs`
- Modify: `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs`

- [ ] Step 1: Add failing navigation expectations for `Ngôn ngữ`
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement route copy and sidebar item
- [ ] Step 4: Run focused tests and confirm they pass

### Task 5: Replace translation review screen with matrix workflow

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Admin/TranslationReview.razor`
- Add: `src/NarrationApp.Web/Pages/Admin/TranslationReview.razor.css`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Admin/TranslationReviewTests.cs`

- [ ] Step 1: Add failing UI tests for KPI cards, matrix rows, and auto-translate all
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement the matrix page with existing shared shell patterns
- [ ] Step 4: Run focused tests and confirm they pass

### Task 6: Adjust audio management to depend on saved translations

**Files:**
- Modify: `src/NarrationApp.Web/Pages/Admin/AudioManagement.razor`
- Modify: `src/NarrationApp.Web/Pages/Admin/AudioManagement.razor.css`
- Modify: `tests/NarrationApp.Web.Tests/Pages/Admin/AudioManagementTests.cs`

- [ ] Step 1: Add failing UI tests for translation-driven generate buttons and disabled states
- [ ] Step 2: Run focused tests and confirm failures
- [ ] Step 3: Implement the new workflow and wire it to the new portal service method
- [ ] Step 4: Run focused tests and confirm they pass

### Task 7: Verify the complete slice

**Files:**
- Modify only as needed from previous tasks

- [ ] Step 1: Run `dotnet test D:\\VinhKhanhFoodStreet\\tests\\NarrationApp.Server.Tests\\NarrationApp.Server.Tests.csproj --no-restore`
- [ ] Step 2: Run `dotnet test D:\\VinhKhanhFoodStreet\\tests\\NarrationApp.Web.Tests\\NarrationApp.Web.Tests.csproj --no-restore`
- [ ] Step 3: Fix any regressions until both suites pass
- [ ] Step 4: Summarize assumptions and any deferred follow-up items
