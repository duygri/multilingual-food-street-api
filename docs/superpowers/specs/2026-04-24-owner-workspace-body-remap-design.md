# Owner Workspace Body Remap Design

**Date:** 2026-04-24  
**Scope:** Owner web body experience, owner/admin integration contracts, owner image upload flow  
**Status:** Approved in conversation; pending written spec review

---

## Goal

Remap the **body area** of the six owner screens so they match the approved dark dashboard/list/form style from the new mock set, while still using real backend data and staying linked to the existing admin workflow.

Target screens:

- `/owner/dashboard`
- `/owner/pois`
- `/owner/pois/new`
- `/owner/pois/{id:int}`
- `/owner/moderation`
- `/owner/profile`

Success means:

- owner screens no longer look like the earlier light `panel-shell` concept that the user rejected
- all six screens share one consistent visual system
- data shown in the new UI comes from real owner/admin APIs, not placeholder-only UI composition
- owner actions still flow into admin moderation/audio operations
- owner can upload **one representative image from file** per POI, stored in the backend and visible across owner/admin surfaces

---

## Relationship To Existing Specs

This spec **keeps** the earlier route split and owner shell improvements from:

- `docs/superpowers/specs/2026-04-22-owner-portal-layout-refresh-design.md`

This spec **overrides the body-level page presentation decisions** from that earlier design where they conflict with the newly approved mock set.

Specifically:

- keep route split, sidebar grouping, shell summary, owner profile sidebar card
- keep owner/backend correctness fixes already shipped
- replace the old owner body language (`welcome hero`, light cards, `panel-shell`-driven composition) with the newly approved dark owner workspace system

---

## Product Decisions

### 1. Keep shell and sidebar, replace owner page bodies

- `PortalShell`, owner sidebar, owner top-level shell behavior, and the refreshed owner sidebar profile card remain in place.
- The remap only targets the **page body** for the six approved owner screens.
- The shell is not rewritten to duplicate the mock's top-right search/bell/avatar chrome.

### 2. Build one shared owner body system, not six disconnected page rewrites

- The six owner pages must feel like one product family.
- Shared body primitives should define the owner visual system:
  - page heading and breadcrumb strip
  - owner stat cards
  - dark data panel
  - owner table/list row
  - owner form section
  - owner status chips and action bars
- Page-specific CSS still exists for truly unique parts, but not for basic card/table/form patterns.

### 3. New UI must stay backed by real data

- Every new surface must render real owner/admin data from backend APIs.
- If an existing endpoint is too thin for the approved UI, add or extend owner-specific workspace DTOs/endpoints.
- Avoid "design-only" placeholders except for explicitly out-of-scope items.

### 4. Owner remains linked to the admin workflow

- Owner moderation views must reflect the same moderation request records that admin reviews.
- Owner audio views must reflect audio assets that admin generates or manages.
- Owner create/detail flows must continue to submit work into the same moderation and content pipeline the admin portal already uses.

### 5. Representative image upload is in scope, gallery is not

- The first iteration supports **one uploaded representative image per POI**.
- The persisted field remains `Poi.ImageUrl`.
- The UI may visually hint at a richer media area, but only one image is supported in this pass.

---

## In Scope

- visual remap of the six owner page bodies
- shared owner body CSS/component primitives
- owner dashboard workspace endpoint
- owner POI list workspace endpoint
- owner POI detail workspace endpoint or equivalent detail data expansion
- owner moderation workspace endpoint
- create/detail file upload flow for one representative image
- test updates across web and server for new owner workspace contracts

---

## Out Of Scope

- remapping `/owner/notifications`
- replacing the global shell/sidebar frame
- full image gallery / multiple images per POI
- rewriting admin layouts to match owner mock visuals
- introducing a separate design system outside the current teal/navy brand language

---

## Current Implementation Anchors

Primary web files:

- `src/NarrationApp.Web/Layout/MainLayout.razor`
- `src/NarrationApp.Web/Layout/MainLayout.razor.css`
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
- `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- `src/NarrationApp.Web/Services/ModerationPortalService.cs`
- `src/NarrationApp.Web/Services/OwnerProfileService.cs`
- `src/NarrationApp.Web/Services/AudioPortalService.cs`

Primary server files:

- `src/NarrationApp.Server/Controllers/OwnerController.cs`
- `src/NarrationApp.Server/Controllers/ModerationRequestsController.cs`
- `src/NarrationApp.Server/Controllers/AudioController.cs`
- `src/NarrationApp.Server/Controllers/AdminController.cs`
- `src/NarrationApp.Server/Services/ModerationService.cs`
- `src/NarrationApp.Server/Services/AudioService.cs`
- `src/NarrationApp.Server/Services/AnalyticsService.cs`
- `src/NarrationApp.Server/Services/IStorageService.cs`

Shared DTO anchors:

- `src/NarrationApp.Shared/DTOs/Owner/OwnerDtos.cs`
- `src/NarrationApp.Shared/DTOs/Poi/PoiDtos.cs`
- `src/NarrationApp.Shared/DTOs/Moderation/ModerationDtos.cs`
- `src/NarrationApp.Shared/DTOs/Audio/AudioDtos.cs`

---

## Visual Direction

The approved mock set defines a body language with:

- dark navy surfaces
- teal/green accents as the system default
- orange/red reserved for warning and rejection emphasis
- strong white numeric hierarchy
- thin borders and soft glows instead of light hero gradients
- dashboard/table/form layouts that read as "operator workspace", not marketing panels

### Non-negotiable visual rules

- remove the bright owner dashboard hero
- avoid returning to white or pale green hero blocks
- keep teal as the primary brand accent
- use dark panels with crisp borders for data-heavy surfaces
- create consistent stat cards and data panels across all six screens

---

## Shared Owner Body System

The owner body system should provide reusable structural classes or lightweight shared components for the following patterns.

### 1. Page heading block

Each owner page body begins with:

- page title
- breadcrumb line
- optional page-level actions

This block lives **inside the page body**, not in the shell header.

### 2. Stat card strip

Shared stat cards render:

- icon tile
- main numeric value
- short label
- optional micro-badge or hint

Use this strip on:

- dashboard
- POI list
- POI detail metrics
- moderation
- profile activity summary

### 3. Dark data panel

Shared panel styling for:

- table blocks
- feed/history blocks
- source content blocks
- forms and grouped settings

The panel needs:

- heading row
- optional action area
- optional inner toolbar
- scroll-safe table region

### 4. Owner table/list system

Data-dense owner screens should prefer a clear table-like structure over stacked freeform cards.

Used by:

- published POI list on dashboard
- full POI list
- moderation queue/history
- audio matrix in POI detail

### 5. Owner form section

Used by:

- create page
- detail edit panels
- profile/account forms

Must support:

- two-column desktop layout
- stacked mobile layout
- section caption + title + help text
- action bar pinned to section or page footer

### 6. Status chips

All status chips share one common tone mapping:

- published / approved / ready: good
- pending / in review: warn
- rejected: danger
- neutral / absent / draft: muted

---

## Page Mapping

## 1. Dashboard

### Remove

- bright welcome hero
- right-column stacked white metric capsules
- old `SystemStatusRail`
- old `Moderation watch` card wording

### New structure

1. page title + breadcrumb
2. stat card strip:
   - total POI
   - published
   - pending review
   - audio ready
3. two-column content grid:
   - left: `POI đã xuất bản`
   - right: `Hoạt động gần đây`

### Dashboard data requirements

The page needs real data for:

- summary counts
- list of published POIs with category, listen count, and trend spark bars
- recent activity feed tied to owner/admin actions

### Recommended contract

Add:

- `GET /api/owner/dashboard/workspace`

Proposed DTO shape:

- `OwnerDashboardWorkspaceDto`
  - `Summary`
  - `PublishedRows`
  - `RecentActivities`

Where:

- `Summary`
  - `TotalPois`
  - `PublishedPois`
  - `PendingReviewPois`
  - `ReadyAudioAssets`
- `PublishedRows`
  - `PoiId`
  - `PoiName`
  - `ImageUrl`
  - `CategoryName`
  - `ListenCount`
  - `Trend`
  - `LocationHint`
- `RecentActivities`
  - `Type`
  - `Title`
  - `Description`
  - `OccurredAtUtc`
  - `Tone`
  - `LinkedPoiId`

### Backend composition

- counts from owner POIs + audio assets + moderation
- listen counts from `VisitEvents`
- trends from grouped recent `VisitEvents`
- activity feed from:
  - owner moderation requests
  - admin review results
  - audio assets becoming ready
  - POI create/update transitions

---

## 2. POI List

### Remove

- old hero block
- card-per-POI list layout as the main representation

### New structure

1. page title + breadcrumb
2. stat card strip:
   - total POI
   - published
   - pending review
   - draft / rejected
3. main dark data panel:
   - inner toolbar with search/filter/create action
   - data table for POIs

### Table columns

- POI
- danh mục
- tọa độ
- priority
- nội dung nguồn
- trạng thái
- thao tác

### POI list data requirements

The list needs row-level source presentation such as:

- `Script TTS`
- `Audio file`
- `Chưa có`

Existing `PoiDto` is too thin to render this confidently for the new table.

### Recommended contract

Add:

- `GET /api/owner/pois/workspace`

Proposed DTO shape:

- `OwnerPoisWorkspaceDto`
  - `Summary`
  - `Rows`

Where each row includes:

- base POI info
- category
- coordinates
- priority
- representative image URL
- `SourceContentKind`
- status
- quick action state such as `CanResubmit`

### Backend composition

- POI base data from `Pois`
- category from joined category
- source content kind inferred from:
  - existing Vietnamese audio asset presence
  - source script presence
- status from POI + moderation state

---

## 3. Create POI

### New structure

1. page title + breadcrumb
2. two-column workspace
   - left: POI information and description
   - right: representative image + source content section
3. bottom action bar:
   - `Lưu nháp`
   - `Gửi duyệt`

### Create behavior

`Lưu nháp`

- creates POI through the existing create path
- keeps POI in draft
- uploads representative image if selected
- uploads source audio if selected
- redirects to detail or keeps editing flow, depending on implementation ergonomics

`Gửi duyệt`

- creates draft POI if needed
- uploads representative image if selected
- uploads source audio if selected
- creates moderation request
- updates page state so admin can immediately see the submission in moderation

### API integration

Reuse:

- `POST /api/pois`
- `POST /api/audio/upload`
- `POST /api/moderation-requests`

Add:

- `POST /api/pois/{id}/image`

Optional helper if the page flow becomes simpler:

- `POST /api/owner/pois/submit`

The preferred first pass is **compose existing create + upload + moderation calls**, not invent a large orchestration endpoint unless the UI complexity forces it.

---

## 4. POI Detail

### Remove

- old preview block styling
- old mixed light section language
- current metrics row phrasing that does not match the new operator-style cards

### New structure

1. summary hero panel
   - thumbnail
   - POI name
   - status badge
   - metadata chips
   - primary actions
2. main two-column content
   - left: source content
   - right: multilingual audio matrix
3. lower stat card strip
   - total listens
   - geofence triggers
   - QR scans
   - listen duration
4. moderation alert card when rejected

### Detail data requirements

The mock requires metrics beyond the current `OwnerPoiStatsDto`.

Needed metrics:

- total visits
- audio plays
- translation count
- audio asset count
- geofence count
- QR scans
- total listen duration

### Recommended contract

Preferred:

- `GET /api/owner/pois/{id}/workspace`

Alternate acceptable implementation:

- extend `GET /api/owner/pois/{id}`
- extend `GET /api/owner/pois/{id}/stats`
- continue joining audio/moderation separately on the client

Because the body remap is extensive, the preferred contract is the dedicated workspace endpoint to keep detail loading explicit and cohesive.

### POI detail workspace DTO

Proposed:

- `OwnerPoiDetailWorkspaceDto`
  - `Poi`
  - `Stats`
  - `AudioItems`
  - `ModerationItems`
  - `AvailableCategories`

### Backend composition

- POI data from owner-owned POI
- audio from `AudioAssets`
- moderation from owner moderation requests filtered to the POI
- QR scans from `VisitEvents` where `EventType == QrScan`
- listen duration from `VisitEvents.ListenDurationSeconds`

### Admin linkage

- audio rows show the same `AudioAssets` that admin generated or updated
- rejected moderation notes come from the same records admin wrote
- owner resubmission continues to create moderation requests that admin sees

---

## 5. Moderation

### Remove

- old hero copy-heavy moderation workspace layout
- old card split that still reads like an intermediate internal prototype

### New structure

1. page title + breadcrumb
2. stat card strip:
   - pending
   - approved
   - rejected
3. moderation process strip
4. `Yêu cầu đang chờ duyệt` data panel
5. `Lịch sử duyệt` data panel

### Moderation data requirements

The mock needs richer moderation rows than the current plain `ModerationRequestDto`.

Needed display values:

- POI name
- request type
- submitted time
- wait time
- review date
- result
- admin note

### Recommended contract

Add:

- `GET /api/owner/moderation/workspace`

Proposed DTO:

- `OwnerModerationWorkspaceDto`
  - `Summary`
  - `PendingRows`
  - `HistoryRows`
  - `FlowState`

### Backend composition

- moderation requests from the same `ModerationRequests` table used by admin review flows
- POI names resolved by joining POI entities
- wait duration derived from request create/review timestamps
- approved/rejected counts derived from owner-owned requests

---

## 6. Profile

### Remove

- old profile hero block
- old mixed metric grid that does not match the new workspace style

### New structure

1. page title + breadcrumb
2. two-column layout
   - left: account information form
   - right top: change password
   - right bottom: activity summary

### Data source

Profile can continue to reuse:

- `GET /api/owner/profile`
- `PUT /api/owner/profile`
- `POST /api/auth/change-password`

The backend already provides enough profile information for this first remap.

### Behavior to keep

- after profile save, refresh auth session so shell/sidebar owner identity stays correct

---

## Representative Image Upload

## Product rule

- only one representative image per POI in this pass
- upload source is a local file chosen by the owner
- persisted target remains `Poi.ImageUrl`

## Backend contract

Add:

- `POST /api/pois/{id}/image`
- `DELETE /api/pois/{id}/image`

### Upload behavior

- request type: `multipart/form-data`
- authorized roles: `poi_owner`, `admin`
- owner must own the POI unless admin
- accepted formats: JPG, PNG, WEBP
- size limit enforced
- file saved via `IStorageService`, matching the audio storage pattern
- old image file deleted when replaced, when possible
- response returns updated `PoiDto`

### Why this shape

- reuses an existing storage abstraction already used by audio
- keeps admin and owner reading from the same POI image field
- avoids introducing a separate image table or gallery subsystem

## Web behavior

Create page:

- choose image file before or after POI creation
- once POI exists, upload image and update preview

Detail page:

- replace current URL-only image editing with file upload UI
- optional fallback to manual URL may be removed or retained only as a hidden implementation convenience

POI list/dashboard/detail:

- all image thumbnails read from the same `Poi.ImageUrl`

---

## Admin Integration

The owner remap must stay coupled to the admin system in these ways:

### 1. Moderation

- owner `submit for review` creates the same moderation request records that appear in admin moderation queues
- owner moderation history shows the same review outcomes and notes that admin produced

### 2. Audio

- owner detail audio matrix shows the same audio assets that admin generates from TTS or translations
- owner source content remains the upstream input for admin audio generation

### 3. POI data

- owner image upload updates the shared POI record
- admin POI pages can see the same representative image and base metadata

### 4. Activity feed

- owner dashboard activity feed should include meaningful admin-originating events:
  - POI approved
  - POI rejected
  - audio ready
  - request submitted

---

## API Strategy

Use a mixed strategy:

### Reuse existing endpoints where the current contract is already good enough

- profile read/update
- password change
- create/update/delete POI
- audio upload
- create moderation request

### Add owner workspace endpoints where the mock requires pre-composed data

Add:

- `GET /api/owner/dashboard/workspace`
- `GET /api/owner/pois/workspace`
- `GET /api/owner/moderation/workspace`
- `GET /api/owner/pois/{id}/workspace`

This keeps the Blazor pages from reconstructing operator-grade tables and feeds out of many thin calls.

---

## Web Service Additions

Expected additions in web service interfaces:

- `IOwnerPortalService`
  - `GetDashboardWorkspaceAsync()`
  - `GetPoisWorkspaceAsync()`
  - `GetPoiWorkspaceAsync(int poiId)`
  - `UploadPoiImageAsync(int poiId, ...)`
  - `DeletePoiImageAsync(int poiId)`

Moderation/profile/audio services may stay separate if their current boundaries remain clear.

---

## Testing Strategy

## Server tests

Add or extend tests for:

- owner dashboard workspace endpoint
- owner POI list workspace endpoint
- owner POI detail workspace endpoint
- owner moderation workspace endpoint
- POI image upload authorization and persistence
- POI image replacement/removal behavior
- QR scan and listen-duration metrics in detail stats/workspace

## Web tests

Update bUnit coverage so each owner page is asserted against the new body layout:

- dashboard stat cards + published table + activity panel
- POI list stat cards + table rows + toolbar actions
- create page two-column form + draft/submit actions
- detail page summary hero + audio matrix + stat cards + reject surface
- moderation page stats + flow strip + pending/history tables
- profile page account/password/activity layout

Keep regression coverage for:

- create draft
- submit review
- save profile and refresh auth session
- detail refresh when audio/moderation changes
- sidebar summary refresh flows

## Verification

Minimum verification before completion:

- focused server owner tests
- focused web owner tests
- full server suite
- full web suite

---

## Rollout Notes

Recommended implementation order:

1. shared owner body system
2. dashboard workspace endpoint + dashboard remap
3. POI list workspace endpoint + POI list remap
4. image upload backend contract
5. create/detail remap with image flow
6. moderation workspace endpoint + moderation remap
7. profile remap

This order delivers visible user value early while progressively tightening backend contracts.

---

## Risks And Constraints

### 1. Existing page tests are copy-sensitive

The body remap will intentionally break wording/structure assertions. Tests must be updated in the same slice.

### 2. Workspace endpoints can drift if duplicated logic is scattered

Owner metrics and activity composition should be centralized enough that dashboard/list/detail do not each reimplement slightly different counting logic.

### 3. Image upload adds storage lifecycle concerns

Replacing a representative image should not leak orphaned files when storage deletion is possible.

### 4. Mock fidelity must not create shell duplication

The shell remains the shell. The body should adopt the mock's structure without cloning the mock's outer app chrome inside page content.

---

## Final Approved Direction

The approved direction for implementation is:

- keep the existing owner shell and sidebar
- replace the body of the six owner screens with a unified dark operator-style workspace system
- back the new UI with real owner/admin data
- add owner workspace endpoints where existing contracts are too thin
- support one representative image uploaded from file per POI, stored through backend infrastructure and shared with admin views

