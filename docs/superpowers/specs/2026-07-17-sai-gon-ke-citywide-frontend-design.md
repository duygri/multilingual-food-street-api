# Sài Gòn Kể — Citywide Frontend Redesign

**Date:** 2026-07-17  
**Status:** Approved design  
**Scope:** .NET MAUI Blazor Hybrid visitor app first; brand alignment for Blazor Web Admin and Owner portals

## 1. Objective

Reposition the product from a Vĩnh Khánh / District 4 experience into **Sài Gòn Kể**, a citywide Ho Chi Minh City discovery product where food is the entry point and local culture is the storytelling layer.

The redesign must:

- make the mobile visitor experience feel newly designed rather than recolored;
- support discovery across Ho Chi Minh City without hard-coded Vĩnh Khánh or District 4 assumptions in the UI;
- preserve the existing audio, multilingual narration, GPS, proximity, QR, cache, directions, notification, and tour capabilities;
- align Admin and Owner portals with the new brand without turning operational screens into editorial layouts;
- remain usable when GPS, network access, images, or current-area content are unavailable.

The visitor redesign targets `NarrationApp.Mobile`, which is a .NET MAUI Blazor Hybrid app. `NarrationApp.Web` does not host a mobile-browser visitor experience; it contains Admin, Owner, authentication, and public QR portal surfaces. Native permission, safe-area, notification, and keyboard requirements therefore apply to the MAUI WebView host. The Web project only receives the portal alignment described in Section 10.

## 2. Product Positioning

### Brand

- **Name:** Sài Gòn Kể
- **Tagline:** Nghe chuyện phố, nếm vị Sài Gòn.
- **Positioning:** Food-first city discovery with neighborhood, people, and cultural stories layered into each place and journey.

### Content model

Food remains the primary discovery hook. Cultural context appears through place descriptions, narrated stories, curated journeys, neighborhoods, markets, people, and local history. The UI must not imply that the catalog is limited to one food street or one district.

## 3. Scope

### In scope

- Mobile onboarding, Discover, Search, Map, Place Detail, Journeys, Player, and Settings surfaces.
- A new mobile information architecture and shared visual system.
- New brand name, tagline, content labels, empty states, and citywide copy in Vietnamese and English.
- Removal of structural emoji icons in favor of one vector icon family.
- Citywide map defaults and theme/category filters derived from existing API data.
- Replacement of Vĩnh Khánh-centric fallback/demo presentation with representative citywide examples.
- Basic Admin and Owner brand alignment: brand labels, color tokens, typography, and citywide copy.
- Responsive, accessibility, and regression coverage appropriate to the affected surfaces.

### Out of scope

- Backend schema changes.
- A new content recommendation engine.
- New geocoding, district-boundary, or editorial CMS services.
- Rebuilding operational Admin/Owner workflows.
- Fabricating production citywide catalog coverage when the API only contains a smaller data set.

## 4. Chosen Design Direction

### Visual direction: Editorial Sài Gòn

Use a warm editorial identity that suggests food, print storytelling, and contemporary Saigon without becoming nostalgic decoration.

The chosen direction combines:

- the warmth and distinctiveness of the Editorial Sài Gòn concept;
- the clear map and audio states of the Urban Night concept;
- the breathing room and usability of the Local Calm concept.

Rejected alternatives:

- **Urban Night:** strong for map/audio but too close to the current dark, technology-led UI and less distinctive as a food-and-culture brand.
- **Local Calm:** highly usable but reads closer to wellness or eco discovery than Saigon food storytelling.

### Discovery structure: Hybrid City Lens

The default Discover screen combines a compact nearby map preview with an editorial feed. It gives immediate geographic context without making a dense map the first and only experience.

Rejected alternatives:

- **Story Feed only:** strong storytelling but weak city-scale spatial context.
- **Map First:** clear geography but overwhelming for new users and weak as a brand introduction.

## 5. Mobile Information Architecture

Use four bottom-navigation destinations:

1. **Khám phá / Discover**
   - default landing surface after onboarding;
   - current-location or neutral city context;
   - search entry;
   - compact City Lens map preview;
   - nearby stories ordered deterministically: `Priority` descending, then GPS-derived distance ascending when a valid location is available, then stable POI ID ascending; without valid GPS, omit the distance comparator and use `Priority` descending followed by stable POI ID ascending;
   - theme/category filters derived from available API data;
   - city-scale spatial discovery through CityLens and Map.

2. **Bản đồ / Map**
   - full-screen city map;
   - marker clustering at city scale;
   - individual POIs at closer zoom levels;
   - shared place bottom sheet with image, optional neutral location label, distance, narration duration, directions, and listening actions.

3. **Hành trình / Journeys**
   - renames the current Tour destination in the UI;
   - curated routes with duration, stop count, difficulty, and progress;
   - retains existing tour session and progression behavior.

4. **Của tôi / My**
   - app language;
   - audio and GPS settings;
   - offline content/cache;
   - listening history;
   - about and application information.

The primary visitor flow is:

`Open app → see nearby story context → preview spatial context → open a place → listen or navigate`

## 6. Visual System

### Color tokens

| Role | Value | Usage |
|---|---:|---|
| Brand primary | `#9A3412` | Primary CTA, selected place, active brand state |
| Background | `#FFFBEB` | Main warm canvas |
| Foreground | `#0F172A` | Primary text and high-contrast content |
| Surface | `#FFFFFF` | Cards, sheets, operational surfaces |
| Audio/success | `#047857` | Audio, GPS, success, secondary action states |
| Cultural accent | `#854D0E` | Editorial labels and cultural emphasis |
| Muted foreground | `#475569` | Secondary metadata on light surfaces |
| Border | `#F2E6E2` | Surface separation |
| Destructive | `#DC2626` | Errors and destructive actions only |
| Focus ring | `#7C2D12` | Keyboard/accessibility focus indication |

All semantic foreground/background pairs must meet WCAG AA for their rendered text size. Production components may use only the semantic tokens in this table; concept-only colors are not valid implementation tokens. Brand primary, foreground, audio/success, cultural accent, and destructive colors may be used for normal text on the listed light surfaces only after an automated contrast assertion and a rendered-device check. Small metadata defaults to `Foreground` or a separately verified muted-foreground token, not to an accent color.

### Typography

- **Display:** Lora for editorial heroes, journey names, and story headlines.
- **UI/body:** Be Vietnam Pro for navigation, controls, metadata, map labels, settings, and portals.
- Base body size is at least 16px with 1.5–1.7 line height.
- Serif must not be used for dense data, small labels, forms, or navigation.
- Fonts must be bundled or have a reliable local fallback so offline mobile use remains legible.
- Bundled Lora and Be Vietnam Pro files must include the complete Vietnamese character subset and be verified with representative Vietnamese copy on a physical or emulated device.

### Imagery and icons

- Use real food, street, market, and people photography as the primary visual material.
- Use a single vector icon family with consistent stroke and optical size.
- Remove emoji used as navigation, category, audio, language, or system icons.
- Image fallbacks use branded geometric surfaces and accessible text, not emoji.
- Avoid heavy retro textures, fake paper effects, and decorative overlays that reduce legibility.

### Motion and interaction

- Touch targets are at least 44×44px with at least 8px between adjacent targets.
- Standard motion duration is 150–250ms.
- Animate opacity and transform rather than layout dimensions.
- Respect `prefers-reduced-motion`.
- Fixed navigation, sheets, and audio controls respect safe areas and never obscure scroll content.

## 7. Core Components

### CityLens

A compact static map snapshot on Discover that communicates nearby density and opens the full Map surface. It reuses the existing client-side map runtime in a non-interactive configuration and does not introduce a new static-map service. CityLens does not support pan, zoom, pinch, or marker-level interaction, so it cannot capture the vertical feed gesture. The whole preview is one accessible tap target labeled to open Map. It occupies 28–35% of the initial viewport at supported phone sizes.

### StoryCard

Displays a real image or branded fallback, place name, theme/category, optional neutral location label, distance when available, narration duration/status, and one clear action. The whole card may open detail, while secondary actions must remain distinct and accessible.

### JourneyCard

Displays the existing tour cover image when available, name, stop count, duration, difficulty label, and current progress when active. Theme and route-distance metadata are omitted in this release because `TourDto` does not provide authoritative values for them.

### ThemeChip

Filter controls for categories/themes already provided by `CategoryDto` and mapped to `VisitorCategory`. An `AreaChip` is intentionally not included in this frontend-only release because `PoiDto` currently contains coordinates but no authoritative district/neighborhood field. Citywide spatial discovery remains available through search, CityLens, and the full Map. Area filtering can be added later only after the API provides trustworthy area metadata; the frontend must not infer districts from POI names or coarse latitude rules.

### AudioDock

A compact persistent player that appears only when relevant. It must coexist with bottom navigation, safe-area insets, the keyboard, and modal sheets.

### PlaceSheet

A shared bottom sheet for map preview and place context. It provides a predictable route to detail, listening, and directions.

### Feedback states

Reusable loading, offline, empty, image-error, map-error, and API-error patterns with a reason and an actionable next step.

## 8. Data and State

The existing API contracts and `VisitorShellState` remain the source of truth. The redesign introduces presentation selectors/formatters where necessary rather than duplicating domain state.

```text
API / cache / GPS
        ↓
VisitorShellState and existing services
        ↓
City Lens / Story Feed / Map / Journeys
        ↓
Place Detail → Audio / Directions / Offline
```

Requirements:

- remove hard-coded Vĩnh Khánh and District 4 assumptions from user-facing brand copy, default map framing, filters, and empty states;
- use `CategoryDto` / `VisitorCategory` as the authoritative source for theme filters;
- stop using the current `BuildAreaLabel` name/latitude heuristics as district truth; until the API exposes an authoritative area field, show a neutral city label or omit area metadata rather than guessing a district;
- center on the user when GPS is available and fall back to a citywide Ho Chi Minh City view otherwise;
- preserve actual API truth: the UI must not claim complete citywide coverage;
- keep existing audio, GPS, proximity, QR, notification, directions, offline cache, and tour progression services intact;
- maintain a clearly labeled, code-owned non-production `VisitorContentSnapshot.CreateDemo()` set with illustrative POIs across representative areas for offline/demo states. These records are demo fixtures only, never merged into live API results, never counted as catalog coverage, and must not cause the UI to claim production citywide availability.

## 9. Resilience and Empty States

- **Loading:** fixed-size skeletons reserve card and image space to prevent layout shift.
- **Offline:** cached content remains usable; show last-sync context where available.
- **GPS denied/unavailable:** allow manual exploration through search, themes, and the city map, and explain which nearby features are unavailable.
- **No filter results:** offer clear-filter and change-theme actions.
- **Image failure:** show branded fallback without layout collapse.
- **Map failure:** keep list discovery available and provide retry.
- **API failure:** prefer cached/fallback content with explicit source status; otherwise provide retry and a useful empty state.
- **No citywide content yet:** describe the currently available catalog honestly rather than showing a false coverage claim.

## 10. Admin and Owner Alignment

Admin and Owner remain operational, light, data-first portals.

Changes are limited to:

- `Sài Gòn Kể Admin`, `Sài Gòn Kể Đối tác`, and neutral portal brand labels;
- shared primary, focus, border, and typography tokens;
- removal of Vĩnh Khánh/District 4 assumptions in explanatory copy and analytics empty states;
- citywide terminology where the underlying visualization already operates on returned coordinates;
- no editorial hero layouts, oversized serif headings, or consumer-style cards in dense workflows.

## 11. Accessibility and Responsive Requirements

- WCAG AA contrast for text and essential controls.
- Logical heading hierarchy and screen-reader reading order.
- Accessible labels for icon-only controls and map actions.
- Selected, expanded, playing, loading, offline, and disabled state semantics must not rely on color alone.
- Predictable back behavior across overlays, detail screens, full player, and settings subpages.
- Test at 375px, a large phone viewport, phone landscape, tablet portrait/landscape, and desktop portal sizes.
- No horizontal page scrolling.
- Keyboard/search input must not cover results or navigation.
- Dynamic text growth must not clip core actions.

## 12. Verification Strategy

### Automated

- Update/add component tests for navigation labels, brand copy, filters, loading, empty, offline, error, and selected states.
- Add presentation-selector tests for category/theme derivation, neutral area-label behavior, and citywide fallback behavior.
- Update tests that assert legacy brand labels or District 4-only UI copy.
- Run the existing Web and Mobile test suites to protect audio, GPS, proximity, QR, cache, directions, notifications, and tour behavior.

### Visual and manual

- Render or run the mobile shell at required viewport sizes.
- Verify Discover, Map, Journeys, Place Detail, full player, and Settings.
- Verify Admin and Owner brand alignment without loss of information density.
- Test safe areas, keyboard-open layouts, AudioDock + bottom navigation, long Vietnamese/English copy, offline/error states, reduced motion, and contrast.

## 13. Implementation Boundaries

- Incrementally extract focused components and presentation helpers from the existing partial `Home.razor` implementation. Do not run a parallel replacement shell or a big-bang rewrite; each extracted surface must replace the existing path and keep tests green.
- Reuse the current service and state boundaries.
- Centralize semantic visual tokens; do not scatter raw colors across components.
- Keep the redesign incremental enough that tests can validate each surface, while the delivered visitor experience must feel cohesive and complete.
- Backend or content-operations gaps discovered during implementation must be documented rather than hidden with fabricated UI behavior.
- Rename `Tour` to `Hành trình / Journeys` only at the presentation layer. Existing enum values, deep-link targets, cache keys, API routes, session identifiers, and analytics event identifiers remain unchanged.

## 14. Success Criteria

The work is complete when:

- a new visitor sees **Sài Gòn Kể**, not a renamed Vĩnh Khánh app;
- Discover provides both nearby spatial context and editorial food/culture stories;
- Map and theme navigation work sensibly at city scale;
- no primary user-facing surface assumes that all content belongs to Vĩnh Khánh or District 4;
- existing visitor capabilities continue to work;
- Admin and Owner visibly belong to the same brand while remaining operational tools;
- responsive, accessibility, and regression checks pass with recorded evidence.
