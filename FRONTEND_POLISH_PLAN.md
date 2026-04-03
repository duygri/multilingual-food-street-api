# Frontend Polish Vinh Khanh Implementation Plan

> Bản này là shortcut ở root project. Nguồn chính vẫn là `docs/superpowers/plans/2026-04-03-frontend-polish-vinh-khanh.md`.

## Mục tiêu

Làm giao diện web và mobile trông mượt, gọn, chuyên nghiệp hơn mà không đổi hướng sản phẩm hay đập bỏ kiến trúc hiện tại.

## Hướng làm

- Giữ nguyên Blazor layouts, pages và flow hiện có
- Xây lại lớp trình bày theo 3 tầng:
  - design tokens dùng chung
  - shell/layout rõ ràng
  - polish theo từng feature quan trọng như map, POI detail, dashboard, form
- Ưu tiên cảm giác sản phẩm thật, đơn giản, sạch, dễ nhìn

## Các task chính

### 1. Establish Visual System
- Tạo `tokens.css`
- Tạo `utilities.css`
- Chuẩn hóa màu, spacing, radius, shadow, font, motion
- Nối CSS mới vào web và mobile

### 2. Polish Admin Shell
- Làm lại sidebar, top header, breadcrumb, nav states
- Giảm cảm giác “template admin”
- Tối ưu responsive cơ bản

### 3. Upgrade Admin and Owner Surface Components
- Polish `Dashboard`
- Polish `Locations`
- Polish `MyStore`
- Polish `MenuManagement`
- Polish `TtsConsole`
- Polish `Stats`

### 4. Refresh Public Map and POI Experience
- Làm overlay map gọn hơn
- Làm lại hero POI detail
- Nâng audio section
- Polish `Tours` và `TourDetail`

### 5. Upgrade Mobile Shell and Mobile Readability
- Làm mobile header + tab bar chắc hơn
- Tối ưu spacing và card trên mobile
- Làm POI detail dễ đọc hơn
- Tinh chỉnh companion UI của native map

### 6. Motion, Feedback, and Micro-Interactions
- Chuẩn hóa animation tokens
- Thêm transitions có chủ đích
- Dọn loading, empty, error states

### 7. Vendor and Asset Cleanup
- Dọn warning `Chart.js CDN`
- Lên backlog đổi `google.maps.Marker` sang `AdvancedMarkerElement`
- Chuẩn hóa title/meta/favicons

### 8. Responsive QA and Visual Regression Checklist
- Viết visual guideline
- Viết QA checklist cho desktop/mobile/map/audio/tour

## File plan đầy đủ

[2026-04-03-frontend-polish-vinh-khanh.md](C:/Users/letro/source/repos/PROJECT%20C%23/docs/superpowers/plans/2026-04-03-frontend-polish-vinh-khanh.md)

