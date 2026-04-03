# Frontend Polish Vinh Khanh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Làm toàn bộ giao diện web và mobile trông mượt, gọn, chuyên nghiệp hơn mà không đổi hướng sản phẩm hay đập bỏ kiến trúc hiện tại.

**Architecture:** Giữ nguyên Blazor layouts, pages và flow hiện có; tập trung xây lại lớp trình bày theo 3 tầng: design tokens dùng chung, shell/layout rõ ràng, và polish theo từng feature quan trọng như map, POI detail, dashboard, form. Mọi thay đổi UI phải đi theo hướng thống nhất visual language giữa admin web, web public và mobile, thay vì vá riêng lẻ từng trang.

**Tech Stack:** Blazor WebAssembly, .NET MAUI Blazor Hybrid, Razor components, CSS trong `SharedUI`, Google Maps JS, native Android map, Chart.js

---

## File Structure

**Thiết lập nền tảng giao diện**
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\tokens.css`
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\utilities.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\app.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\admin.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Frontend\wwwroot\index.html`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\index.html`

**Shell và layout**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Layout\AdminLayout.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Layout\MobileLayout.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Layout\MainLayout.razor`

**Public experience**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Explore.razor` if present
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\TourDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Tours.razor`

**Admin/owner experience**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Dashboard.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MenuManagement.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\TtsConsole.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Stats.razor`

**JS/UI support**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\chart-helper.js`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\tts-service.js`

**Docs / QA**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\README.md`
- Create: `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\specs\2026-04-03-frontend-visual-guidelines.md`

---

### Task 1: Establish Visual System

**Files:**
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\tokens.css`
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\utilities.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\app.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Frontend\wwwroot\index.html`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\index.html`

- [ ] **Step 1: Tạo bộ design tokens thống nhất**
  Bao gồm màu chủ đạo, màu semantic, spacing, shadow, radius, font, transition, z-index, container width, breakpoint.

- [ ] **Step 2: Chuyển token cứng trong `app.css` sang biến dùng chung**
  Loại bỏ màu lẻ tẻ và shadow rời rạc xuất hiện nhiều nơi.

- [ ] **Step 3: Tạo utility classes tối thiểu**
  Ví dụ: `surface-card`, `section-title`, `pill-badge`, `stack-gap`, `soft-divider`, `skeleton-block`.

- [ ] **Step 4: Chuẩn hóa typography**
  Giữ một font sans chính, scale heading rõ ràng, line-height đồng nhất, letter-spacing nhẹ cho title.

- [ ] **Step 5: Nối stylesheet mới vào web và mobile**
  Đảm bảo `tokens.css` và `utilities.css` được load trước CSS page-specific.

- [ ] **Step 6: Build kiểm tra**
  Run: `dotnet build "C:\Users\letro\source\repos\PROJECT C#\SharedUI\FoodStreet.UI.csproj" --no-restore`
  Expected: `0 error`

- [ ] **Step 7: Commit**
  `git commit -m "feat: add shared frontend design tokens"`

---

### Task 2: Polish Admin Shell

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Layout\AdminLayout.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\admin.css`

- [ ] **Step 1: Thu gọn visual language của sidebar và header**
  Giảm cảm giác “template admin”, chuyển sang bề mặt sáng sạch hơn, điểm nhấn cam/đỏ ấm hợp Vĩnh Khánh.

- [ ] **Step 2: Thiết kế lại vùng header**
  Làm breadcrumb, title, search, notification, profile rõ cấp bậc hơn và bớt chật.

- [ ] **Step 3: Chuẩn hóa nav states**
  Active, hover, focus, unread, disabled phải nhất quán.

- [ ] **Step 4: Tối ưu khoảng trắng**
  Giảm sự nặng nề của padding lớn và card dày.

- [ ] **Step 5: Làm responsive cho admin shell**
  Ở màn nhỏ hơn desktop lớn, sidebar nên collapse hoặc giảm width hợp lý.

- [ ] **Step 6: Build kiểm tra**
  Run: `dotnet build "C:\Users\letro\source\repos\PROJECT C#\Frontend\FoodStreet.Client.csproj" --no-restore`
  Expected: `0 error`

- [ ] **Step 7: Commit**
  `git commit -m "feat: polish admin shell visual system"`

---

### Task 3: Upgrade Admin and Owner Surface Components

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Dashboard.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MenuManagement.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\TtsConsole.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Stats.razor`

- [ ] **Step 1: Chuẩn hóa card stats**
  Cùng chiều cao, cùng spacing, icon treatment nhẹ, metric hierarchy rõ.

- [ ] **Step 2: Chuẩn hóa table/list/filter bar**
  Filters nằm trên một thanh chung, action buttons gọn, không dùng nhiều style lẻ.

- [ ] **Step 3: Tối ưu modal và form**
  Nhãn, help text, validation, image picker, map picker, translation modal phải gọn và dễ scan.

- [ ] **Step 4: Làm TTS Console nhìn như tool chuyên dụng**
  Chia rõ `health`, `quick test`, `queue`, `results`.

- [ ] **Step 5: Tối ưu chart/stats area**
  Thống nhất kích thước chart canvas, legend, empty state, loading skeleton.

- [ ] **Step 6: Build kiểm tra**
  Run: `dotnet build "C:\Users\letro\source\repos\PROJECT C#\Frontend\FoodStreet.Client.csproj" --no-restore`
  Expected: `0 error`

- [ ] **Step 7: Commit**
  `git commit -m "feat: polish admin and owner feature surfaces"`

---

### Task 4: Refresh Public Map and POI Experience

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\TourDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Tours.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js`

- [ ] **Step 1: Làm map overlay tinh gọn**
  Status badge, focus card, recenter button, filter chips phải nhìn “product” hơn “debug”.

- [ ] **Step 2: Làm lại POI detail hero**
  Hero image, title, category, language/audio state, quick actions cần có hierarchy rõ hơn.

- [ ] **Step 3: Nâng phần audio section**
  Language chips, play state, fallback state, loading state rõ và đẹp hơn.

- [ ] **Step 4: Nâng phần menu và tour**
  Section headings, stop cards, CTA `start/resume/progress` nhất quán với card system mới.

- [ ] **Step 5: Chuẩn hóa empty/loading/error state**
  Không để khối trắng trống; dùng skeleton hoặc messaging ngắn, rõ.

- [ ] **Step 6: Đánh giá lại marker and popup treatment**
  Nếu chưa chuyển `AdvancedMarkerElement`, ít nhất phải làm marker/icon/card nhất quán về visual.

- [ ] **Step 7: Build kiểm tra**
  Run: `dotnet build "C:\Users\letro\source\repos\PROJECT C#\Frontend\FoodStreet.Client.csproj" --no-restore`
  Expected: `0 error`

- [ ] **Step 8: Commit**
  `git commit -m "feat: polish public map and poi experience"`

---

### Task 5: Upgrade Mobile Shell and Mobile Readability

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Layout\MobileLayout.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\layout\activity_native_map.xml` if needed

- [ ] **Step 1: Tinh chỉnh mobile header và tab bar**
  Bớt “demo app”, tăng cảm giác native và chắc tay.

- [ ] **Step 2: Chuẩn hóa spacing một tay**
  Các vùng chạm, nút, chip, card trên mobile phải đủ lớn và đều nhau.

- [ ] **Step 3: Tối ưu POI detail trên màn hẹp**
  Audio, menu, map preview, actions không bị dồn và không quá dài.

- [ ] **Step 4: Tối ưu native map companion UI**
  Focus card, meta text, button labels, recenter/open actions phải rõ hơn.

- [ ] **Step 5: Kiểm tra portrait mobile flow**
  `explore -> poi -> map -> directions -> back` phải không bị vỡ layout.

- [ ] **Step 6: Build kiểm tra**
  Run: `dotnet build "C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj" -f net9.0-android --no-restore`
  Expected: `0 error`

- [ ] **Step 7: Commit**
  `git commit -m "feat: polish mobile shell and poi readability"`

---

### Task 6: Motion, Feedback, and Micro-Interactions

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\app.css`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\css\admin.css`
- Modify: relevant Razor pages that have async actions

- [ ] **Step 1: Chuẩn hóa animation tokens**
  Fast, normal, slow; hover, panel-open, card-rise, fade-in.

- [ ] **Step 2: Thêm transitions có chủ đích**
  Sidebar hover, filter chip, modal, notification panel, CTA buttons, map focus card.

- [ ] **Step 3: Dọn loading states**
  Spinner dùng tiết chế; ưu tiên skeleton hoặc text trạng thái ngắn.

- [ ] **Step 4: Dọn error messaging**
  Dùng tone ngắn, rõ, không quá kỹ thuật ở nơi user-facing.

- [ ] **Step 5: Tối ưu focus/keyboard states**
  Đảm bảo accessibility cơ bản vẫn đẹp.

- [ ] **Step 6: Commit**
  `git commit -m "feat: add polished feedback and motion states"`

---

### Task 7: Vendor and Asset Cleanup

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Frontend\wwwroot\index.html`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\index.html`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\chart-helper.js`
- Create if needed: local vendor file strategy note

- [ ] **Step 1: Dọn external asset warnings**
  Đánh giá Chart.js CDN; nếu cần, chuyển local để tránh Edge tracking-prevention noise.

- [ ] **Step 2: Dọn map warning strategy**
  Lên backlog chuyển `google.maps.Marker` sang `AdvancedMarkerElement`.

- [ ] **Step 3: Dọn favicon/title/meta**
  Tên app, mô tả, title nên phản ánh đúng Vĩnh Khánh multilingual storytelling.

- [ ] **Step 4: Commit**
  `git commit -m "chore: clean frontend asset loading and metadata"`

---

### Task 8: Responsive QA and Visual Regression Checklist

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\README.md`
- Create: `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\specs\2026-04-03-frontend-visual-guidelines.md`

- [ ] **Step 1: Viết visual checklist**
  Cover desktop admin, tablet, mobile, map, POI detail, tour, TTS console.

- [ ] **Step 2: Viết naming rules cho future UI**
  Card types, section headers, button variants, status badge usage.

- [ ] **Step 3: Thêm smoke test checklist**
  `admin login`, `locations`, `map`, `poi detail`, `tts console`, `mobile map`, `tour resume`.

- [ ] **Step 4: Commit**
  `git commit -m "docs: add frontend visual guidelines and qa checklist"`

---

## Recommended Execution Order

1. Task 1 — dựng visual system
2. Task 2 — shell admin
3. Task 3 — surface admin/owner
4. Task 4 — public map + POI
5. Task 5 — mobile shell
6. Task 6 — motion and feedback
7. Task 7 — vendor cleanup
8. Task 8 — docs + QA

## Success Criteria

- Giao diện nhìn đồng bộ giữa admin, web public và mobile
- Các trang quan trọng không còn cảm giác “prototype/debug”
- Khoảng trắng, card, nút, typography nhất quán
- Map và audio UI dễ hiểu hơn, ít khối thô
- Không phát sinh regression auth/map/mobile runtime
- Build sạch cho `SharedUI`, `Frontend`, và `Mobile`

