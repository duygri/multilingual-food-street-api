# Owner Portal Layout Refresh Design

**Date:** 2026-04-22  
**Scope:** Owner web portal and owner-specific backend contracts  
**Status:** Approved in conversation; pending written spec review

---

## Goal

Tổ chức lại owner portal để rõ bố cục, rõ luồng vận hành, và bám tinh thần của `owner-dashboard-demo.html` cùng `owner_review_report.md`, nhưng vẫn giữ nguyên design system xanh ngọc, shared shell, và pattern Blazor hiện có trong project.

Kết quả mong muốn:

- owner nhìn vào portal là biết ngay việc gì cần làm
- mỗi màn có một nhiệm vụ rõ ràng thay vì dồn nhiều trách nhiệm vào hai page lớn
- không fork theme hay tạo một design system riêng cho owner
- mở rộng backend đúng chỗ để `notifications`, `profile`, `moderation`, và `poi detail` dùng dữ liệu thật

---

## Current Implementation Anchors

Các file hiện tại là nền để refactor:

- `src/NarrationApp.Web/Layout/MainLayout.razor`
- `src/NarrationApp.Web/Layout/MainLayout.razor.css`
- `src/NarrationApp.SharedUI/Components/PortalShell.razor`
- `src/NarrationApp.SharedUI/Components/PortalShell.razor.css`
- `src/NarrationApp.Web/Pages/Owner/Dashboard.razor`
- `src/NarrationApp.Web/Pages/Owner/Dashboard.razor.css`
- `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor`
- `src/NarrationApp.Web/Pages/Owner/PoiManagement.razor.css`
- `src/NarrationApp.Web/Services/OwnerPortalService.cs`
- `src/NarrationApp.Web/Services/IOwnerPortalService.cs`
- `src/NarrationApp.Web/Services/NotificationCenterApiService.cs`
- `src/NarrationApp.Web/Services/AuthClientService.cs`
- `src/NarrationApp.Server/Controllers/OwnerController.cs`
- `src/NarrationApp.Server/Controllers/NotificationsController.cs`
- `src/NarrationApp.Server/Controllers/AuthController.cs`
- `src/NarrationApp.Server/Services/AuthService.cs`
- `src/NarrationApp.Server/Data/Entities/AppUser.cs`

---

## Product Decisions

### 1. Keep the shared design system

- Giữ nguyên tông xanh ngọc và token hiện tại.
- Không đổi brand color owner sang cam như file demo.
- Ưu tiên tái sử dụng `PortalShell`, `PanelShell`, `MetricCard`, `StatusBadge`, `SystemStatusRail`, `NotificationCenter`.

### 2. Match the demo through information architecture, not pixel-copying

- Mục tiêu là chuyển tinh thần demo thành cấu trúc Blazor rõ ràng hơn.
- Không sao chép `owner-dashboard-demo.html` theo kiểu 1:1 HTML/CSS.
- Các khác biệt hợp lý với demo được chấp nhận nếu giúp tái dùng component và codebase hiện tại.

### 3. Split owner responsibilities into focused routes

- Không giữ mô hình “`Dashboard` + `PoiManagement` ôm toàn bộ owner flow”.
- Mỗi route phải có một trách nhiệm rõ ràng: tổng quan, danh sách, tạo mới, chi tiết, moderation, notifications, profile.

### 4. Extend backend where the current contract is too thin

- `notifications` dùng dữ liệu thật từ API hiện có.
- `profile` được phép mở rộng backend để bám gần demo hơn.
- `dashboard`, `moderation`, và `poi detail` sẽ dùng DTO tổng hợp để FE không phải tự ghép quá nhiều nguồn nhỏ.

### 5. Optimize for clarity before micro-interactions

- Ưu tiên bố cục, hierarchy, trạng thái loading/empty/error, confirm flows, toast feedback.
- Animation, ripple, và polish chỉ là lớp sau, không phải trọng tâm iteration này.

---

## Information Architecture

### Owner route map

Route owner mới:

- `/owner/dashboard`
- `/owner/pois`
- `/owner/pois/new`
- `/owner/pois/{id:int}`
- `/owner/moderation`
- `/owner/notifications`
- `/owner/profile`

Route chuyển tiếp:

- `/owner/poi-management`
  - giữ lại tạm thời như alias hoặc redirect sang `/owner/pois`
  - tránh gãy bookmark hoặc flow cũ trong test/manual usage

### Sidebar grouping

Sidebar owner được chia thành bốn nhóm:

- `Tổng quan`
  - `Dashboard`
- `Nội dung`
  - `POI`
  - `Tạo POI mới`
- `Vận hành`
  - `Moderation`
  - `Notifications`
- `Tài khoản`
  - `Profile`

### Header behavior

Header của owner vẫn chạy trong `MainLayout`, nhưng có behavior rõ hơn theo route:

- page title + summary theo route
- ô search theo ngữ cảnh page khi phù hợp
- primary action theo page
- shortcut notification ở header vẫn giữ, nhưng không thay thế page `Notifications`

---

## Owner Shell Layout

### Sidebar profile card

`PortalShell` cần hỗ trợ một owner profile card ngay dưới brand khi user có role `poi_owner`.

Card này hiển thị:

- `FullName`
- role label `POI Owner`
- số `POI`
- số `Published`
- số `Pending`

Dữ liệu dùng:

- `FullName` từ auth/session
- số liệu dashboard từ owner summary

### Navigation badges

Sidebar item cần hỗ trợ badge count cho:

- `Moderation` = pending review count
- `Notifications` = unread notification count

### Shared shell changes

`PortalShell` nên được mở rộng bằng slot hoặc parameter thay vì hard-code riêng cho owner:

- optional profile card region
- optional search region
- badge count per navigation item
- richer header action region

Điều này giữ component đủ generic để admin vẫn dùng được mà không bị phân nhánh theme.

---

## Page Design

### 1. Dashboard

Mục tiêu:

- cho owner thấy tình hình vận hành trong một lần nhìn
- nêu bật việc cần làm ngay

Nội dung chính:

- `welcome banner`
  - chào owner theo tên
  - tóm tắt số POI đang quản lý
  - số pending moderation
  - số notifications chưa đọc
- `stat cards`
  - giữ 6 chỉ số hiện có nếu dữ liệu còn phù hợp
- `POI spotlight / published overview`
  - danh sách POI nổi bật hoặc mới cập nhật
  - hiển thị status, danh mục, geofence, narration readiness, metrics chính
- `activity feed`
  - gửi duyệt
  - bị từ chối
  - được duyệt
  - audio ready
  - cập nhật POI
- `moderation watch`
  - các mục đang chờ duyệt
  - CTA đi sang page moderation

Dashboard không nên chứa editor hay geofence form.

### 2. POI list

Route:

- `/owner/pois`

Mục tiêu:

- owner nắm toàn bộ POI theo trạng thái
- tìm, lọc, và mở chi tiết nhanh

Nội dung chính:

- toolbar:
  - search theo tên hoặc slug
  - filter trạng thái
  - CTA `Tạo POI mới`
- list/table:
  - tên
  - slug
  - danh mục
  - status
  - geofence summary
  - translation summary
  - narration summary
  - quick action `Xem chi tiết`

Page này không chứa form chỉnh sửa inline dài như hiện tại.

### 3. Create POI

Route:

- `/owner/pois/new`

Mục tiêu:

- tạo nháp mới trong một màn riêng, tách khỏi danh sách

Section đề xuất:

- thông tin cơ bản
- vị trí và danh mục
- image upload area
- mô tả nguồn
- TTS script
- audio nguồn
- geofence mặc định

Behavior:

- tạo xong điều hướng sang `/owner/pois/{id}`
- loading và validation phải rõ theo section

### 4. POI detail

Route:

- `/owner/pois/{id:int}`

Mục tiêu:

- là nơi xem nhanh tình trạng một POI và chỉnh sửa có ngữ cảnh

Structure:

- `preview card` phía trên
  - ảnh
  - tên POI
  - status badge
  - meta pills: category, coordinates, radius, priority, POI ID
  - action buttons: lưu, gửi duyệt, mở bản đồ, xóa
- các panel bên dưới:
  - thông tin chỉnh sửa
  - geofence editor
  - audio language table
  - thống kê POI
  - moderation snapshot

Behavior quan trọng:

- nếu POI đang `Rejected`, hiển thị rejection reason nổi bật
- có CTA `Sửa & gửi lại`
- không để owner phải mở page moderation mới biết lý do bị từ chối

### 5. Moderation

Route:

- `/owner/moderation`

Mục tiêu:

- thể hiện vòng đời duyệt theo cách dễ hiểu hơn timeline text rời rạc

Nội dung chính:

- visual stepper của flow moderation
- danh sách POI đang chờ duyệt
- các POI vừa được xử lý gần đây
- moderation history table
  - POI
  - loại yêu cầu
  - ngày gửi
  - ngày phản hồi
  - kết quả
  - note từ admin

Page này là nơi theo dõi vận hành kiểm duyệt, không phải nơi chỉnh sửa nội dung chi tiết.

### 6. Notifications

Route:

- `/owner/notifications`

Mục tiêu:

- thay popover bằng một page lịch sử thông báo đầy đủ

Nội dung chính:

- summary bar với unread count
- filter theo loại notification
- list thông báo
- action `mark read`
- action `mark all read`

Behavior:

- vẫn dùng `NotificationCenterApiService`
- page tự refresh theo realtime event khi có notification mới

### 7. Profile

Route:

- `/owner/profile`

Mục tiêu:

- cho owner quản lý hồ sơ và bảo mật tài khoản trong một page riêng

Nội dung chính:

- `owner profile`
  - full name
  - email
  - phone
  - managed area
  - role
  - preferred language
- `change password`
  - current password
  - new password
  - confirm password
- `activity summary`
  - ngày tạo tài khoản
  - tổng POI
  - số POI published
  - lượt nghe
  - lần đăng nhập cuối

Rule:

- `email` và `role` là read-only
- `fullName`, `phone`, `managedArea`, `preferredLanguage` là editable

---

## Data and API Changes

### 1. Auth/session enrichment

Mở rộng `AuthResponse` và mapping session để shell có đủ dữ liệu nhận diện:

- thêm `FullName`

Không nhồi toàn bộ profile owner vào auth response.

### 2. AppUser fields

Mở rộng `AppUser` với các field cần thiết cho profile owner:

- `Phone`
- `ManagedArea`
- `LastLoginAtUtc`
- `CreatedAtUtc` nếu chưa có metadata tương đương sẵn sàng dùng

`LastLoginAtUtc` được cập nhật trong login flow của `AuthService`.

### 3. Owner profile contracts

Thêm DTO và request riêng cho owner profile:

- `OwnerProfileDto`
- `UpdateOwnerProfileRequest`
- `OwnerActivitySummaryDto`

Endpoints:

- `GET /api/owner/profile`
- `PUT /api/owner/profile`

`POST /api/auth/change-password` tiếp tục được dùng cho form đổi mật khẩu.

### 4. Dashboard contracts

Giữ `GET /api/owner/dashboard`, nhưng mở rộng response để không chỉ trả số liệu KPI.

DTO đề xuất:

- `OwnerDashboardDto`
  - summary metrics
  - owner greeting info
  - spotlight POIs
  - activity feed
  - moderation watch summary

Nếu DTO hiện tại trở nên quá lớn, tách:

- `GET /api/owner/dashboard`
- `GET /api/owner/dashboard/activity`

Ưu tiên đầu tiên là page DTO rõ nghĩa, không ép FE ghép dữ liệu quá tay.

### 5. POI detail contracts

Thêm owner detail endpoint:

- `GET /api/owner/pois/{id}`

DTO đề xuất:

- `OwnerPoiDetailDto`
  - POI core info
  - geofences
  - stats
  - moderation summary
  - rejection note
  - audio language statuses

Audio language table dùng:

- `OwnerPoiAudioStatusDto`
  - `LanguageCode`
  - `DisplayName`
  - `AudioType`
  - `Status`
  - `DurationSeconds`
  - `HasTranslation`
  - `HasAudio`

### 6. Moderation contracts

Thêm endpoint moderation tổng hợp cho owner:

- `GET /api/owner/moderation`

DTO đề xuất:

- `OwnerModerationOverviewDto`
  - flow stepper state
  - pending items
  - recent decisions
  - history items

History row dùng:

- `OwnerModerationHistoryItemDto`

### 7. Notifications

Notifications tiếp tục dùng contract hiện có:

- `GET /api/notifications`
- `GET /api/notifications/unread-count`
- `PUT /api/notifications/{id}/read`
- `PUT /api/notifications/read-all`

Không tạo API owner-specific song song nếu behavior không khác.

---

## Web Service Structure

### Owner services

Mở rộng `IOwnerPortalService` và `OwnerPortalService` để phục vụ owner pages:

- dashboard aggregate
- POI list
- POI detail
- moderation overview

### Owner profile service

Thêm service riêng:

- `IOwnerProfileService`
- `OwnerProfileService`

Trách nhiệm:

- load owner profile
- update owner profile
- bridge change password flow nếu cần wrapper riêng cho page

### Auth service boundary

`AuthClientService` giữ trách nhiệm:

- login
- register
- register owner
- load current session
- logout

Không đẩy logic owner profile chi tiết vào `AuthClientService`.

---

## UI Component Structure

### Shared UI additions

Trong `src/NarrationApp.SharedUI/Components`, thêm các component generic dùng lại được:

- `ToastHost`
- `ConfirmDialog`

`PortalShell` được mở rộng để hỗ trợ:

- profile card region
- search region
- badge count per navigation item

### Owner-specific components

Chỉ tách component owner-specific khi page markup đủ lớn để cần chia:

- `OwnerActivityFeed`
- `OwnerModerationStepper`
- `OwnerAudioLanguageTable`
- `OwnerProfileSummaryCard`

Các component này nên đặt trong khu web owner thay vì shared UI nếu admin không dùng lại.

### Page files

`src/NarrationApp.Web/Pages/Owner` sẽ có:

- `Dashboard.razor`
- `Dashboard.razor.css`
- `Pois.razor`
- `Pois.razor.css`
- `PoiCreate.razor`
- `PoiCreate.razor.css`
- `PoiDetail.razor`
- `PoiDetail.razor.css`
- `Moderation.razor`
- `Moderation.razor.css`
- `Notifications.razor`
- `Notifications.razor.css`
- `Profile.razor`
- `Profile.razor.css`

`PoiManagement.razor` được giữ tạm trong giai đoạn chuyển tiếp rồi loại bỏ khi route mới ổn định.

---

## UX and Interaction Rules

### Loading, empty, and error states

Mỗi owner page phải có đủ bốn trạng thái:

- loading
- empty
- success
- API error

### Confirm flows

Phải có confirm dialog cho:

- xóa POI
- gửi duyệt
- các action profile có rủi ro nếu cần

### Toast feedback

Toast dùng cho:

- lưu POI thành công/thất bại
- upload audio
- gửi duyệt
- đổi mật khẩu
- cập nhật profile
- mark all notifications as read

### Rejected POI handling

POI bị từ chối phải hiển thị:

- rejection note rõ ràng
- trạng thái hiện tại
- CTA `Sửa & gửi lại`

### Search and filtering

- search/filter ở `POI list` chạy client-side trước
- chỉ chuyển sang server-side filtering nếu volume dữ liệu thực tế bắt đầu gây chậm

---

## Testing Strategy

### Web tests

Mở rộng `tests/NarrationApp.Web.Tests` theo pattern đang có:

- `Components/PortalShellTests.cs`
  - owner profile card
  - nav badges
- `Pages/Owner/DashboardTests.cs`
  - welcome banner
  - activity feed
  - moderation watch
- `Pages/Owner/PoisTests.cs`
  - filter/search
  - điều hướng sang create/detail
- `Pages/Owner/PoiCreateTests.cs`
  - render section form
  - submit create success/error
- `Pages/Owner/PoiDetailTests.cs`
  - preview card
  - rejection note
  - audio language table
- `Pages/Owner/ModerationTests.cs`
  - stepper
  - history table
- `Pages/Owner/NotificationsTests.cs`
  - mark read
  - mark all read
- `Pages/Owner/ProfileTests.cs`
  - update profile
  - change password validation

Service tests:

- `Services/OwnerPortalServiceTests.cs`
- `Services/OwnerProfileServiceTests.cs`
- update `Services/AuthClientServiceTests.cs` for `FullName`

### Server tests

Mở rộng `tests/NarrationApp.Server.Tests` cho:

- owner profile endpoints
- owner moderation aggregate endpoint
- owner POI detail endpoint
- dashboard activity projection
- `AuthService` cập nhật `LastLoginAtUtc`
- validation cho update owner profile

Không đưa visual regression framework vào iteration này.

---

## Non-Goals for This Iteration

- không đổi design system owner sang màu cam
- không redesign admin portal theo owner layout
- không làm drag-and-drop upload ảnh phức tạp hơn mức cần thiết nếu upload cơ bản đã đáp ứng
- không thêm animation nặng hoặc hiệu ứng chỉ để giống demo
- không tối ưu phân trang/server filtering sớm khi dữ liệu owner còn nhỏ

---

## Implementation Order

1. Mở rộng auth/session với `FullName` và metadata owner cần cho shell.
2. Mở rộng `AppUser` cùng owner profile API.
3. Mở rộng `PortalShell` và `MainLayout` để owner có profile card, nav badges, search/action slots.
4. Tách route owner mới và thêm redirect/alias cho `/owner/poi-management`.
5. Refactor `Dashboard` thành layout mới với welcome banner, spotlight, activity feed, moderation watch.
6. Tách `POI list`, `Create POI`, và `POI detail`.
7. Thêm page `Moderation`, `Notifications`, `Profile`.
8. Bổ sung toast/confirm shared components và cắm vào owner flows.
9. Cập nhật web tests và server tests cho toàn bộ flow owner mới.

---

## Success Criteria

Thiết kế này được xem là hoàn tất khi:

- owner portal có route map rõ ràng thay vì hai page gánh toàn bộ trách nhiệm
- shell owner vẫn dùng design system xanh ngọc hiện tại
- `notifications` và `profile` trở thành page riêng dùng dữ liệu thật
- `dashboard`, `moderation`, và `poi detail` thể hiện thông tin vận hành rõ hơn hiện tại
- các action quan trọng có confirm/loading/toast phù hợp
- test suite web/server có coverage cho behavior mới ở mức đủ tự tin để refactor tiếp
