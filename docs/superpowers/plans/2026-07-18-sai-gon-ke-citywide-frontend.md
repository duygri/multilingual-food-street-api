# Sài Gòn Kể Citywide Frontend Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver the approved “Sài Gòn Kể” citywide frontend redesign in the MAUI Blazor Hybrid visitor app and align the Blazor Web Admin/Owner portals without changing backend contracts or breaking audio, GPS, proximity, QR, cache, directions, notifications, or tour sessions.

**Architecture:** Keep `VisitorShellState` and the existing services as the source of truth, add small presentation selectors/components for the Hybrid City Lens experience, and extend the existing Mapbox runtime with an explicit non-interactive preview mode plus city-scale clustering. Centralize semantic tokens and vector icons, then apply a lighter subset of the same identity to the shared portal shell. Do not add area inference, backend schema, recommendation, geocoding, or editorial CMS work.

**Tech Stack:** .NET 8/9, .NET MAUI Blazor Hybrid, Blazor WebAssembly, Razor components, C#, CSS, JavaScript/Mapbox GL JS, xUnit, bUnit/source-contract tests.

**Approved spec:** `docs/superpowers/specs/2026-07-17-sai-gon-ke-citywide-frontend-design.md`

**Baseline evidence:** `NarrationApp.Web.Tests` 445 passed; `NarrationApp.Server.Tests` 162 passed on 2026-07-18. Run test projects sequentially because they share `.artifacts` output paths.

---

## File and responsibility map

- `src/NarrationApp.Mobile/wwwroot/css/mobile-foundation.css`: mobile semantic tokens, bundled font faces, shared controls, focus and reduced-motion rules.
- `src/NarrationApp.Mobile/wwwroot/css/mobile-discovery.css`: Discover, CityLens, StoryCard, Place Detail visual composition.
- `src/NarrationApp.Mobile/wwwroot/css/mobile-map.css`: full Map, clusters, overlays, PlaceSheet.
- `src/NarrationApp.Mobile/wwwroot/css/mobile-tours.css`: Journeys list/detail and progress presentation.
- `src/NarrationApp.Mobile/wwwroot/css/mobile-settings.css`: My/settings surfaces.
- `src/NarrationApp.Mobile/wwwroot/css/mobile-shell.css`: bottom navigation, AudioDock positioning, phone/tablet/landscape layout.
- `src/NarrationApp.Mobile/Components/Shared/VisitorIcon.razor`: single inline-SVG structural icon source.
- `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorCityLens.razor`: static, one-tap map preview.
- `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorStoryCard.razor`: reusable POI summary card.
- `src/NarrationApp.Mobile/Features/Home/VisitorPoiPresentationOrder.cs`: deterministic priority/distance/ID ordering.
- `src/NarrationApp.Mobile/Features/Home/VisitorContentMapper.Pois.cs`: maps API POIs without guessing districts.
- `src/NarrationApp.Mobile/Features/Home/VisitorContentSnapshot.cs`: clearly labeled offline/demo fixture only.
- `src/NarrationApp.Mobile/wwwroot/js/visitorMap.js`: interactive full map, non-interactive CityLens mode, city-scale clusters.
- `src/NarrationApp.SharedUI/wwwroot/styles/design-system.css`: portal-facing Sài Gòn Kể semantic tokens.
- `src/NarrationApp.Web/Layout/MainLayout*.cs`: portal brand/copy only.
- `tests/NarrationApp.Web.Tests/Mobile/*`: mobile presentation, state, map runtime, markup, CSS and regression contracts.
- `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs` and `PortalStylesTests.cs`: Admin/Owner branding and shared portal design-system contracts.

---

### Task 1: Establish semantic tokens, bundled Vietnamese fonts, and vector icons

**Files:**
- Create: `src/NarrationApp.Mobile/wwwroot/fonts/Lora-SemiBold.woff2`
- Create: `src/NarrationApp.Mobile/wwwroot/fonts/BeVietnamPro-Regular.woff2`
- Create: `src/NarrationApp.Mobile/wwwroot/fonts/BeVietnamPro-SemiBold.woff2`
- Create: `src/NarrationApp.Mobile/wwwroot/fonts/README.md`
- Create: `src/NarrationApp.Mobile/Components/Shared/VisitorIcon.razor`
- Modify: `src/NarrationApp.Mobile/Components/_Imports.razor`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-foundation.css`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorShellModels.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorCategoryPresentationFormatter.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileStylesTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileProjectConfigurationTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`

- [ ] **Step 1: Write failing design-foundation tests**

Add assertions for the approved tokens, bundled `@font-face` declarations, visible `:focus-visible`, `prefers-reduced-motion`, 44px minimum controls, and the absence of emoji-based structural icon markup.

```csharp
[Fact]
public void Mobile_foundation_defines_sai_gon_ke_tokens_and_accessibility_guards()
{
    var css = ReadMobileCss("mobile-foundation.css");
    Assert.Contains("--sgk-brand: #9a3412", css, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("--sgk-canvas: #fffbeb", css, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("--sgk-audio: #047857", css, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("@font-face", css, StringComparison.Ordinal);
    Assert.Contains("font-family: \"Be Vietnam Pro\"", css, StringComparison.Ordinal);
    Assert.Contains(":focus-visible", css, StringComparison.Ordinal);
    Assert.Contains("@media (prefers-reduced-motion: reduce)", css, StringComparison.Ordinal);
    Assert.Contains("min-height: 44px", css, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run the focused tests and verify failure**

Run:

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MobileStylesTests|FullyQualifiedName~MobileProjectConfigurationTests|FullyQualifiedName~MobileSectionMarkupTests"
```

Expected: FAIL because the new tokens/fonts/icon component do not exist.

- [ ] **Step 3: Add licensed Vietnamese font assets and semantic CSS**

Use the official OFL distributions from the Google Fonts repositories (`ofl/lora` and `ofl/bevietnampro`), include the complete Vietnamese subset, record the exact source commit URL, original filename, OFL license file, and computed SHA-256 for every bundled WOFF2 in `src/NarrationApp.Mobile/wwwroot/fonts/README.md`, and define only semantic tokens in component CSS.

```css
@font-face {
    font-family: "Be Vietnam Pro";
    src: url("../fonts/BeVietnamPro-Regular.woff2") format("woff2");
    font-weight: 400;
    font-display: swap;
}

:root {
    --sgk-brand: #9a3412;
    --sgk-brand-strong: #7c2d12;
    --sgk-canvas: #fffbeb;
    --sgk-surface: #ffffff;
    --sgk-ink: #0f172a;
    --sgk-muted: #475569;
    --sgk-audio: #047857;
    --sgk-cultural: #854d0e;
    --sgk-border: #f2e6e2;
    --sgk-danger: #dc2626;
    --sgk-focus: #7c2d12;
    --sgk-font-display: "Lora", Georgia, serif;
    --sgk-font-ui: "Be Vietnam Pro", system-ui, sans-serif;
}
```

- [ ] **Step 4: Add the shared vector icon component**

Implement an inline SVG switch with a consistent `24×24` viewBox and stroke width for the finite icon set used in this release (`discover`, `map`, `journey`, `user`, `search`, `language`, `refresh`, `location`, `audio`, `directions`, `close`, `notification`, `history`, `download`, `settings`). Unknown names render no glyph and retain an accessible wrapper only when `Label` is supplied.

```razor
<svg class="visitor-icon @Class" viewBox="0 0 24 24"
     aria-hidden="@(string.IsNullOrWhiteSpace(Label) ? "true" : null)"
     aria-label="@Label" role="@(string.IsNullOrWhiteSpace(Label) ? null : "img")">
    @IconPath
</svg>
```

Change `VisitorCategory.MarkerLabel` semantics from emoji content to an icon key and update `VisitorCategoryPresentationFormatter` to return stable vector icon names.

- [ ] **Step 5: Run focused tests and the font/emoji source audit**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MobileStylesTests|FullyQualifiedName~MobileProjectConfigurationTests|FullyQualifiedName~MobileSectionMarkupTests"
rg --pcre2 -n "[\x{2600}-\x{27BF}\x{1F300}-\x{1FAFF}]" src/NarrationApp.Mobile -g '*.razor' -g '*.cs'
```

Expected: tests PASS; the audit lists remaining screens to migrate in Tasks 3–7 but no new structural emoji usage.

- [ ] **Step 6: Commit the foundation**

```powershell
git add src/NarrationApp.Mobile/Components/Shared src/NarrationApp.Mobile/Components/_Imports.razor src/NarrationApp.Mobile/Features/Home/VisitorShellModels.cs src/NarrationApp.Mobile/Features/Home/VisitorCategoryPresentationFormatter.cs src/NarrationApp.Mobile/wwwroot/fonts src/NarrationApp.Mobile/wwwroot/css/mobile-foundation.css tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: add Sai Gon Ke mobile design foundation"
```

---

### Task 2: Make POI presentation citywide, honest, and deterministic

**Files:**
- Create: `src/NarrationApp.Mobile/Features/Home/VisitorPoiPresentationOrder.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorContentMapper.Pois.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorContentSnapshot.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorShellState.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorShellState.Content.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorPoiPresentationOrderTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorContentServiceTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorShellStateTests.cs`

- [ ] **Step 1: Write failing ordering and neutral-location tests**

Cover these exact rules:

- with valid GPS: priority descending → distance ascending → ID ascending;
- without GPS: priority descending → ID ascending;
- API mapping never guesses Q1/Q4 from name or latitude;
- demo fixtures remain fallback-only and span representative city examples without being merged into live results.
- the runtime default tab is `VisitorTab.Discover`, while QR/deep-link handlers may still switch explicitly to Map or Tours.

```csharp
[Fact]
public void Order_WithGps_UsesPriorityThenDistanceThenStableId()
{
    var ordered = VisitorPoiPresentationOrder.Apply(Pois(), hasValidGps: true);
    Assert.Equal(["poi-high-near", "poi-high-far", "poi-low"], ordered.Select(x => x.Id));
}

[Fact]
public void Map_DoesNotInferDistrictFromNameOrLatitude()
{
    var poi = Assert.Single(VisitorContentMapper.Map([CreatePoi("Xóm Chiếu", 10.76, 106.70)], [], [], null).Pois);
    Assert.Equal("TP.HCM", poi.District);
}
```

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~VisitorPoiPresentationOrderTests|FullyQualifiedName~VisitorContentServiceTests|FullyQualifiedName~VisitorShellStateTests"
```

Expected: FAIL because ordering helper and neutral mapping do not exist.

- [ ] **Step 3: Implement the pure ordering selector**

Keep sorting outside `VisitorShellState` mutation logic and return a new array.

```csharp
public static IReadOnlyList<VisitorPoi> Apply(IReadOnlyList<VisitorPoi> pois, bool hasValidGps) =>
    (hasValidGps
        ? pois.OrderByDescending(p => p.Priority).ThenBy(p => p.DistanceMeters).ThenBy(p => p.Id, StringComparer.Ordinal)
        : pois.OrderByDescending(p => p.Priority).ThenBy(p => p.Id, StringComparer.Ordinal))
    .ToArray();
```

Apply it when content/location changes, before the existing category/search filter.

Set `VisitorShellState.CurrentTab` to `VisitorTab.Discover` by default and add a regression test proving `CreateRuntimeDefault()` lands on Discover after onboarding. Keep explicit deep-link/navigation tab switches unchanged.

- [ ] **Step 4: Remove district inference and refresh demo fixtures**

Replace `BuildAreaLabel` heuristics with the neutral `"TP.HCM"` label. Update `CreateDemo()` to clearly named, illustrative non-production records across multiple representative parts of the city, keep `IsFallback=true`, and replace emoji category marker labels with vector icon keys. Do not touch server seed data or merge demo fixtures into API results.

- [ ] **Step 5: Run focused tests**

Expected: PASS with no `BuildAreaLabel` name/latitude branches remaining.

- [ ] **Step 6: Commit the presentation truth layer**

```powershell
git add src/NarrationApp.Mobile/Features/Home tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: make visitor discovery citywide and deterministic"
```

---

### Task 3: Rebrand onboarding, navigation, and shell copy

**Files:**
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorUiTextCatalog.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorUiTextExtensions.cs`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorSetupFlow.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.razor`
- Modify: `src/NarrationApp.Mobile/wwwroot/index.html`
- Modify: `src/NarrationApp.Mobile/NarrationApp.Mobile.csproj`
- Modify: `src/NarrationApp.Mobile/Resources/AppIcon/appicon.svg`
- Modify: `src/NarrationApp.Mobile/Resources/AppIcon/appiconfg.svg`
- Modify: `src/NarrationApp.Mobile/Resources/Splash/splash.svg`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-shell.css`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorUiTextCatalogTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/HomeMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileProjectConfigurationTests.cs`

- [ ] **Step 1: Write failing brand and navigation tests**

Assert Vietnamese/English titles, tagline, four presentation labels, the exact Discover → Map → Journeys → My DOM order, and the absence of Vĩnh Khánh strings in production mobile source.

```csharp
Assert.Equal("Sài Gòn Kể", vi.PageTitle);
Assert.Equal("Hành trình", vi.TabTours);
Assert.Equal("Của tôi", vi.TabMe);
Assert.Equal("Sai Gon Ke", en.PageTitle);
```

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~VisitorUiTextCatalogTests|FullyQualifiedName~HomeMarkupTests|FullyQualifiedName~MobileProjectConfigurationTests"
```

- [ ] **Step 3: Update bilingual copy and presentation-only labels**

Use “Sài Gòn Kể / Sai Gon Ke”, “Nghe chuyện phố, nếm vị Sài Gòn.”, “Hành trình / Journeys”, and “Của tôi / My”. Preserve `VisitorTab.Tours`, QR target kinds, cache keys, routes, session IDs, and analytics identifiers.

- [ ] **Step 4: Refresh the shell and brand assets**

Replace structural emoji in onboarding and navigation with `VisitorIcon`; reorder the rendered bottom navigation to Discover → Map → Journeys → My and style it with the approved cream/terracotta system and safe-area math. Update app title, icon, splash, HTML title/theme color, and accessible names. Do not alter permission or onboarding state transitions. `HomeMarkupTests` must assert the rendered tab order so the underlying legacy enum order cannot silently change presentation.

- [ ] **Step 5: Run focused tests and source audit**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~VisitorUiTextCatalogTests|FullyQualifiedName~HomeMarkupTests|FullyQualifiedName~MobileProjectConfigurationTests"
rg -n -i "Vĩnh Khánh|Vinh Khanh" src/NarrationApp.Mobile
```

Expected: PASS; no production-brand matches.

- [ ] **Step 6: Commit the shell rebrand**

```powershell
git add src/NarrationApp.Mobile tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: rebrand visitor shell as Sai Gon Ke"
```

---

### Task 4: Build Hybrid City Lens Discover

**Files:**
- Create: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorCityLens.razor`
- Create: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorStoryCard.razor`
- Create: `src/NarrationApp.Mobile/Components/Pages/Home.CityLensRuntime.razor.cs`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorDiscoverScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.Runtime.razor.cs`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.Startup.razor.cs`
- Modify: `src/NarrationApp.Mobile/wwwroot/js/visitorMap.js`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-discovery.css`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-shell.css`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/HomeMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorMapScriptTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileStylesTests.cs`

- [ ] **Step 1: Write failing CityLens and Discover composition tests**

Assert:

- Discover contains one search entry, ThemeChips, `VisitorCityLens`, and `VisitorStoryCard`;
- CityLens is one accessible button that opens the full Map tab;
- CityLens map runtime uses a separate `city-lens-map` container and `interactive:false`;
- preview JS disables gesture handling and marker-level buttons;
- CSS constrains preview height to 28–35% of supported initial phone viewports.

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MobileSectionMarkupTests|FullyQualifiedName~HomeMarkupTests|FullyQualifiedName~VisitorMapScriptTests|FullyQualifiedName~MobileStylesTests"
```

- [ ] **Step 3: Extend `visitorMap.render` with explicit options**

Keep current full-map defaults. Add an options argument:

```javascript
const renderOptions = {
  interactive: options?.interactive !== false,
  markersInteractive: options?.markersInteractive !== false,
  showRadius: options?.showRadius !== false,
  cluster: options?.cluster === true
};
```

For CityLens, create the Mapbox instance with interaction disabled, render non-button marker elements, hide radius/route controls, and keep disposal isolated by container ID. Do not add a static-map network service.

- [ ] **Step 4: Implement focused CityLens runtime orchestration**

`Home.CityLensRuntime.razor.cs` builds a snapshot from ordered/filtered POIs, calls `visitorMap.render("city-lens-map", ..., new { interactive = false, markersInteractive = false, showRadius = false })`, and disposes only that surface when leaving Discover. Keep full Map orchestration in `Home.MapRuntime.razor.cs`. Update `Home.Startup.razor.cs::OnAfterRenderAsync` to invoke both lifecycle methods in a stable order:

```csharp
await RenderMapIfNeededAsync();
await RenderCityLensIfNeededAsync();
```

Each method must mount only on its own tab/container and dispose only its own instance; tests assert both calls exist and tab switching cleans the inactive surface.

- [ ] **Step 5: Build the new Discover composition**

Use the Editorial Sài Gòn headline, one search field, horizontally scrollable ThemeChips, CityLens, an honest fallback banner, and ordered StoryCards. Cards show image/fallback, category, neutral city label only when useful, distance only when valid GPS exists, duration, and one clear “Nghe câu chuyện / Listen” affordance. Preserve existing search overlay and POI-detail callbacks.

- [ ] **Step 6: Run focused tests**

Expected: PASS; Discover still opens current search/detail flows.

- [ ] **Step 7: Commit Hybrid City Lens**

```powershell
git add src/NarrationApp.Mobile/Components/Pages src/NarrationApp.Mobile/wwwroot/js/visitorMap.js src/NarrationApp.Mobile/wwwroot/css tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: build hybrid City Lens discovery"
```

---

### Task 5: Make the full Map city-scale and refresh PlaceSheet

**Files:**
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorMapSnapshotBuilder.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorMapRenderState.cs`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorMapScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.MapRuntime.razor.cs`
- Modify: `src/NarrationApp.Mobile/wwwroot/js/visitorMap.js`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-map.css`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorMapSnapshotBuilderTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorMapRenderStateTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorMapScriptTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`

- [ ] **Step 1: Write failing city-map and PlaceSheet tests**

Cover the Ho Chi Minh City fallback center (`10.7769, 106.7009`) with fallback zoom `10.8`, fit-to-returned-points behavior when GPS is unavailable, Mapbox GeoJSON clustering, cluster expansion, unclustered POI callback, and vector-only PlaceSheet structural icons.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement city-scale snapshot behavior**

When there are no markers and no valid GPS, return center `10.7769, 106.7009` at zoom `10.8`. When multiple returned POIs exist, no POI is selected, and GPS is unavailable, JS bounds fitting overrides the fallback center/zoom and fits all returned markers with safe overlay padding. Preserve valid user-location and selected-POI precedence.

- [ ] **Step 4: Add Mapbox clustering without breaking proximity/radius behavior**

Use a GeoJSON point source with `cluster:true`, cluster circle/count layers, and unclustered symbol/circle layers for the general population. Keep selected and nearest POIs visually distinct, retain geofence radius layers, and route clicks through `SelectPoiFromMap`. Cluster clicks call `getClusterExpansionZoom` and ease to the cluster center. Offline fallback retains a non-Mapbox accessible list/marker presentation.

Update `Home.MapRuntime.razor.cs` so the full-map `visitorMap.render` invocation explicitly passes `cluster = true`; CityLens continues to omit it and therefore remains unclustered. `HomeMarkupTests`/`VisitorMapScriptTests` must prove full Map enables clustering while CityLens does not.

- [ ] **Step 5: Recompose PlaceSheet**

Replace emoji metadata with `VisitorIcon`, use image/neutral label/category/distance/audio status, expose Listen as the primary action and Directions/Detail as secondary actions, retain language selection, queue status, directions status, close behavior, and ARIA live regions.

- [ ] **Step 6: Run focused map tests**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~VisitorMapSnapshotBuilderTests|FullyQualifiedName~VisitorMapRenderStateTests|FullyQualifiedName~VisitorMapScriptTests|FullyQualifiedName~MobileSectionMarkupTests"
```

- [ ] **Step 7: Commit the city-scale map**

```powershell
git add src/NarrationApp.Mobile/Features/Home src/NarrationApp.Mobile/Components/Pages/Sections/VisitorMapScreen.razor src/NarrationApp.Mobile/Components/Pages/Home.MapRuntime.razor.cs src/NarrationApp.Mobile/wwwroot/js/visitorMap.js src/NarrationApp.Mobile/wwwroot/css/mobile-map.css tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: scale visitor map across Ho Chi Minh City"
```

---

### Task 6: Refresh Journeys without changing tour identifiers

**Files:**
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.TourPresentation.razor.cs`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorTourListScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorTourDetailScreen.razor`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorContentMapper.Tours.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorContentMapper.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorShellModels.cs`
- Modify: `src/NarrationApp.Mobile/Features/Home/VisitorTourPresentationFormatter.cs`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-tours.css`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorTourPresentationFormatterTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/VisitorTourSessionMapperTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`

- [ ] **Step 1: Write failing Journey presentation tests**

Assert Journey cards use only supported data: resolved cover image when available, title, stop count, duration, difficulty, and progress derived from `ActiveTourSession`. Assert inferred hero tone/icon, route distance, and participation metadata are no longer passed or rendered. Assert UI labels say Hành trình/Journeys while `VisitorTab.Tours`, `VisitorQrTargetKind.Tour`, `tour-*` IDs, session mapping, cache/deep-link strings remain unchanged. Cover both absolute and API-relative `TourDto.CoverImage` values.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Implement editorial Journey cards and detail**

Add `string? CoverImageUrl` to `VisitorTourCard`. Change mapping to `MapTour(tour, assetBaseAddress)` from `VisitorContentMapper.Map`, resolving relative cover paths with the same asset-base rule used for POI images. Update `Home.TourPresentation.razor.cs` to map `ActiveTourSession` progress onto the matching Journey card and update `Home.razor` to pass that supported progress into `VisitorTourListScreen`. Remove existing inferred hero tone/icon, route-distance, and participation wiring. Use Lora for journey names, Be Vietnam Pro for metadata/actions, real cover images or vector fallback, visible progress, predictable back action, and minimum 44px controls.

- [ ] **Step 4: Run focused and tour-session regression tests**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~VisitorTourPresentationFormatterTests|FullyQualifiedName~VisitorTourSessionMapperTests|FullyQualifiedName~MobileSectionMarkupTests|FullyQualifiedName~VisitorShellStateTests"
```

- [ ] **Step 5: Commit Journeys**

```powershell
git add src/NarrationApp.Mobile/Components/Pages/Home.razor src/NarrationApp.Mobile/Components/Pages/Home.TourPresentation.razor.cs src/NarrationApp.Mobile/Components/Pages/Sections src/NarrationApp.Mobile/Features/Home src/NarrationApp.Mobile/wwwroot/css/mobile-tours.css tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: refresh curated journeys presentation"
```

---

### Task 7: Refresh Place Detail, AudioDock/player, Search, and My/settings

**Files:**
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorPoiDetailScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorFullPlayerScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorSearchScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorSettingsOverviewScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorAudioSettingsScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorGpsSettingsScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorCacheManagerScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorListenHistoryScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Sections/VisitorAboutScreen.razor`
- Modify: `src/NarrationApp.Mobile/Components/Pages/Home.razor`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-discovery.css`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-settings.css`
- Modify: `src/NarrationApp.Mobile/wwwroot/css/mobile-shell.css`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileStylesTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/HomeMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/StructuralIconAuditTests.cs`

- [ ] **Step 1: Write failing component-contract tests**

Assert vector icons, semantic headings, image alt text, labeled controls, empty/error/offline actions, AudioDock + bottom-nav safe offsets, keyboard-open search layout, and reduced-motion coverage. Add characterization assertions for the existing QR/deep-link, proximity, audio, cache, directions, notifications, and tour-session callbacks before recomposing their host markup. Preserve all current callbacks and state transitions.

- [ ] **Step 2: Run focused tests and verify failure**

- [ ] **Step 3: Recompose Place Detail and full player**

Apply editorial headline/surface hierarchy, real imagery/fallback, neutral location, category and narration metadata, transcript, related places, sticky actions, and an audio-green player state. Retain language/audio selection, play/pause, transcript, map, directions, and related-POI behavior. Make AudioDock visible whenever the visitor is Ready, `ShowMiniPlayer` is true, a POI is selected, and the full player is closed; hide it only while the Map PlaceSheet is open. It persists across Discover, Map, Journeys, and My, and tab switches must not reset playback state.

- [ ] **Step 4: Refresh Search and My/settings**

Keep Search reachable from Discover, preserve debounced/current selection behavior, provide actionable no-results copy, and prevent the keyboard from covering results. Restyle settings with light data-first surfaces; preserve every existing preference, cache, history, GPS, and about action.

- [ ] **Step 5: Remove all remaining structural emoji and verify safe areas**

```powershell
rg --pcre2 -n "[\x{2600}-\x{27BF}\x{1F300}-\x{1FAFF}]" src/NarrationApp.Mobile -g '*.razor' -g '*.cs'
```

Expected: no structural emoji matches in Razor/C# control markup. `StructuralIconAuditTests` scans these Unicode ranges so the audit cannot miss unlisted glyphs such as map, person, label, timer, footprint, moon, city, or folder emoji. Human-authored API story content is not source markup and is exempt.

- [ ] **Step 6: Run focused tests**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MobileSectionMarkupTests|FullyQualifiedName~MobileStylesTests|FullyQualifiedName~HomeMarkupTests|FullyQualifiedName~VisitorSettingsPresentationFormatterTests|FullyQualifiedName~VisitorSearchResultSelectorTests|FullyQualifiedName~VisitorRelatedPoiSelectorTests"
```

- [ ] **Step 7: Commit the remaining visitor surfaces**

```powershell
git add src/NarrationApp.Mobile/Components/Pages src/NarrationApp.Mobile/wwwroot/css tests/NarrationApp.Web.Tests/Mobile
git commit -m "feat: complete Sai Gon Ke visitor surfaces"
```

---

### Task 8: Align Admin and Owner portals with the new brand

**Files:**
- Modify: `src/NarrationApp.SharedUI/wwwroot/styles/design-system.css`
- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor`
- Modify: `src/NarrationApp.SharedUI/Components/PortalShell.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/AudioPlayer.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/ConfirmDialog.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/EmptyStateBlock.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/MetricCard.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/NotificationCenter.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/PageHeaderPanel.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/StatTile.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/StatusBadge.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/SystemStatusRail.razor.css`
- Modify: `src/NarrationApp.SharedUI/Components/ToastHost.razor.css`
- Modify: `src/NarrationApp.Web/wwwroot/css/app.css`
- Modify: `src/NarrationApp.Web/Pages/Admin/*.razor.css` files returned by the dark-surface audit
- Modify: `src/NarrationApp.Web/Pages/Owner/*.razor.css` files returned by the dark-surface audit
- Modify: `src/NarrationApp.Web/Layout/MainLayout.razor.cs`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.State.razor.cs`
- Modify: `src/NarrationApp.Web/Layout/MainLayout.RouteCopy.razor.cs`
- Modify: `src/NarrationApp.Web/Pages/Admin/Analytics.razor`
- Test: `tests/NarrationApp.Web.Tests/Layout/MainLayoutTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Components/PortalShellTests.cs`
- Test: `tests/NarrationApp.Web.Tests/PortalStylesTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Pages/Admin/AnalyticsTests.cs`

- [ ] **Step 1: Write failing portal brand/token/copy tests**

Assert `Sài Gòn Kể Admin`, `Sài Gòn Kể Đối tác`, neutral portal fallback, the shared terracotta/focus tokens, light portal/form surfaces, and absence of Vĩnh Khánh/Quận 4 emphasis in visible portal copy. Replace the existing `PortalStylesTests` dark-gradient expectations with light semantic surface assertions. Add a source audit test over SharedUI component CSS plus Admin/Owner page CSS that rejects legacy hard-coded navy surface values while allowing explicitly named inverse/overlay semantic tokens.

- [ ] **Step 2: Run focused tests and verify failure**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MainLayoutTests|FullyQualifiedName~PortalShellTests|FullyQualifiedName~PortalStylesTests|FullyQualifiedName~AnalyticsTests"
```

- [ ] **Step 3: Apply the restrained portal identity**

Update brand strings and define light surface, raised surface, overlay, text, muted, border, and explicit inverse-surface/inverse-text tokens. Replace active dark form/surface rules in `src/NarrationApp.Web/wwwroot/css/app.css` with the restrained light semantic system while preserving contrast, focus, validation, disabled, and hover states. Audit every SharedUI component and Admin/Owner page stylesheet returned by `rg -l "rgba\(|linear-gradient"`; replace hard-coded dark panels and coupled light foregrounds with the semantic tokens. Keep only deliberate scrim/overlay and inverse states through their named tokens, including `ConfirmDialog`; verify cards such as `MetricCard`, `EmptyStateBlock`, `StatTile`, page headers, notifications, status rails, and management-page panels all have AA-compatible light surfaces. Keep portal layout, density, tables, forms, navigation, role logic, and workflows unchanged. Use Be Vietnam Pro/system sans only; do not introduce consumer hero layouts or large editorial serif headings.

- [ ] **Step 4: Make Analytics copy honest about current backend scope**

The server currently filters analytics with District 4 bounds. Remove District 4 as the product identity without claiming citywide analytics: use neutral wording such as “phạm vi dữ liệu analytics hiện tại” and document the backend limitation in code comments/tests. Do not change `AnalyticsService` in this frontend task.

- [ ] **Step 5: Run focused portal tests and source audit**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore --filter "FullyQualifiedName~MainLayoutTests|FullyQualifiedName~PortalShellTests|FullyQualifiedName~PortalStylesTests|FullyQualifiedName~AnalyticsTests"
rg -n -i "Vĩnh Khánh|Vinh Khanh|Quận 4" src/NarrationApp.Web src/NarrationApp.SharedUI -g '*.razor' -g '*.cs' -g '*.css'
rg -n "rgba\((?:8, 17, 31|12, 23, 39|15, 27, 46|19, 35, 59)|#(?:08111f|0a1424|0c1728|0f1b2e|13233b)" src/NarrationApp.Web src/NarrationApp.SharedUI -g '*.css'
```

Expected: no legacy brand copy and no legacy hard-coded navy panel surfaces; any remaining District 4 occurrence is a documented backend-scope comment, not visible product positioning. Any deliberate dark scrim/inverse state uses the new named semantic token rather than a literal.

- [ ] **Step 6: Commit portal alignment**

```powershell
git add src/NarrationApp.SharedUI src/NarrationApp.Web/Layout src/NarrationApp.Web/Pages/Admin/Analytics.razor src/NarrationApp.Web/wwwroot/css/app.css tests/NarrationApp.Web.Tests
git commit -m "feat: align portals with Sai Gon Ke identity"
```

---

### Task 9: Complete accessibility, responsive, regression, and visual verification

**Files:**
- Modify as required by failures: affected files from Tasks 1–8 only
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileStylesTests.cs`
- Test: `tests/NarrationApp.Web.Tests/Mobile/MobileSectionMarkupTests.cs`
- Test: `tests/NarrationApp.Web.Tests/PortalStylesTests.cs`
- Create: `docs/superpowers/verification/2026-07-18-sai-gon-ke-frontend.md`

- [ ] **Step 1: Add final automated accessibility/source contracts**

Cover sequential headings, icon-only labels, 44px touch targets, focus visibility, no structural emoji, semantic state text beyond color, reduced motion, safe-area offsets, phone landscape rules, and absence of legacy brand copy.

- [ ] **Step 2: Run complete Web/Mobile tests sequentially**

```powershell
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore
```

Expected: PASS, at least the 445-test baseline plus new tests.

- [ ] **Step 3: Run complete Server regression tests sequentially**

```powershell
dotnet test tests/NarrationApp.Server.Tests/NarrationApp.Server.Tests.csproj --no-restore
```

Expected: PASS, 162/162 or more. Do not run this concurrently with Web tests because they share output paths.

- [ ] **Step 4: Build the supported Android target**

```powershell
dotnet build src/NarrationApp.Mobile/NarrationApp.Mobile.csproj -f net9.0-android35.0 --no-restore
```

Expected: the project’s only supported target builds successfully without new warnings attributable to the redesign.

- [ ] **Step 5: Perform responsive visual QA**

Run the MAUI Android app on an emulator or connected device and use Android/WebView inspection or appropriately sized emulator profiles to verify these states at 375px-equivalent phone portrait, large phone portrait, phone landscape, tablet portrait, and tablet landscape:

- onboarding;
- Discover loading/content/empty/offline/GPS-denied;
- CityLens → full Map;
- clustered Map → PlaceSheet → detail;
- Journey list/detail/active progress;
- AudioDock/full player;
- Search with keyboard open;
- My/settings subpages.

Verify Admin and Owner at 1024px and 1440px widths. Record screenshots or precise observations in the verification document.

- [ ] **Step 6: Verify accessibility and brand audits**

```powershell
rg -n -i "Vĩnh Khánh|Vinh Khanh" src/NarrationApp.Mobile src/NarrationApp.Web src/NarrationApp.SharedUI
rg --pcre2 -n "[\x{2600}-\x{27BF}\x{1F300}-\x{1FAFF}]" src/NarrationApp.Mobile -g '*.razor' -g '*.cs'
git diff --check
```

Expected: no legacy product branding, no structural emoji, no whitespace errors. Document any intentionally retained historical/demo text and why it is not product positioning.

- [ ] **Step 7: Run the UI/UX Pro Max validation pass**

```powershell
python C:\Users\letro\.codex\skills\ui-ux-pro-max\scripts\search.py "animation accessibility z-index loading touch safe-area" --domain ux -n 10
```

Compare the implementation against the returned critical/high rules, with particular attention to contrast, touch size, focus order, overlays, loading feedback, reduced motion, and fixed-element occlusion.

- [ ] **Step 8: Commit verification fixes and evidence**

```powershell
git add src tests docs/superpowers/verification/2026-07-18-sai-gon-ke-frontend.md
git commit -m "test: verify Sai Gon Ke frontend redesign"
```

---

## Completion gate

Do not claim completion until all of the following have direct evidence:

- mobile visitor brand and navigation are Sài Gòn Kể;
- Hybrid City Lens Discover is present and CityLens is non-interactive except for one open-Map action;
- map defaults and clustering work at city scale;
- no guessed district labels or area filters remain;
- audio/GPS/proximity/QR/cache/directions/notifications/tour sessions still pass regression tests;
- all structural emoji are replaced by vector icons;
- Admin/Owner share the brand without consumer-layout regressions;
- responsive/accessibility states are visually checked;
- full Web/Mobile and Server test suites pass sequentially;
- the supported Android mobile build succeeds.
