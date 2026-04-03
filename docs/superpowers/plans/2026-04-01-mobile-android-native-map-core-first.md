# Mobile Android Native Map Core-First Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuyển các flow map cốt lõi trên Android mobile từ WebView JavaScript sang `Maps SDK for Android`, trong khi giữ nguyên web map hiện tại.

**Architecture:** Web tiếp tục dùng `Maps JavaScript API`; mobile Android thêm native map host + bridge service để `SharedUI` gọi qua abstraction. Phase đầu chỉ làm `browse map + focus + directions + picker tọa độ`, chưa kéo `Places autocomplete` và `geofence test` vào cùng lượt.

**Tech Stack:** .NET MAUI Blazor Hybrid, Shared Razor Class Library, Android platform code, Google Maps SDK for Android, existing ASP.NET Core map APIs.

---

## File Structure

### Tạo mới

- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Services\Interfaces\IMobileNativeMapService.cs`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\DTOs\MobileNativeMapRequest.cs`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\DTOs\MobileNativeMapResult.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\AndroidNativeMapService.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\NativeMapActivity.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\NativeMapContracts.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\layout\activity_native_map.xml`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\values\google_maps_api.xml.template`
- `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\plans\2026-04-01-mobile-android-native-map-core-first.md`

### Chỉnh sửa

- `C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\MauiProgram.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\AndroidManifest.xml`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\index.html`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\runtime-config.js`

### Giữ nguyên trong phase này

- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\GeofenceTest.razor`

## Notes Trước Khi Làm

- Repo hiện chưa có Android UI test harness, nên verification chính sẽ là `build + smoke test`.
- Trong bước thêm package binding cho Google Maps Android, phải xác nhận package tương thích với `net9.0-android` trước khi commit sâu hơn. Đây là bước kỹ thuật bắt buộc trước khi làm bridge.
- Không commit key Android thật vào repo. Dùng template/resource placeholder và tài liệu cấu hình cục bộ.

### Task 1: Khóa contract và điểm cắt giữa SharedUI với native Android

**Files:**
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Services\Interfaces\IMobileNativeMapService.cs`
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\DTOs\MobileNativeMapRequest.cs`
- Create: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\DTOs\MobileNativeMapResult.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\MauiProgram.cs`

- [ ] **Step 1: Thiết kế request/result tối thiểu cho phase đầu**

Bao gồm:
- mode `browse` / `picker`
- center mặc định
- user location
- focused POI id
- danh sách marker POI
- kết quả `selectedLatLng`

- [ ] **Step 2: Tạo `IMobileNativeMapService` với API nhỏ, rõ**

Phương thức tối thiểu:
- `OpenBrowseMapAsync(request, cancellationToken)`
- `OpenPickerAsync(request, cancellationToken)`

- [ ] **Step 3: Đăng ký service trong `MauiProgram.cs`**

Nếu chưa có Android implementation thì đăng ký placeholder `NotSupported` cho non-Android branch để compile không gãy.

- [ ] **Step 4: Build `SharedUI` và `Mobile` để xác nhận contract sạch**

Run:
```powershell
dotnet build "C:\Users\letro\source\repos\PROJECT C#\SharedUI\FoodStreet.UI.csproj" --no-restore
dotnet build "C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj" -f net9.0-android --no-restore
```

- [ ] **Step 5: Commit**

```bash
git add SharedUI Mobile
git commit -m "feat: add native mobile map contracts"
```

### Task 2: Dựng native Android map host

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\AndroidManifest.xml`
- Create: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\NativeMapActivity.cs`
- Create: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\NativeMapContracts.cs`
- Create: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\layout\activity_native_map.xml`
- Create: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\values\google_maps_api.xml.template`

- [ ] **Step 1: Thêm binding/package cần thiết cho Google Maps Android**

Xác nhận package binding tương thích với project và thêm vào `FoodStreet.Mobile.csproj`.

- [ ] **Step 2: Khai báo `NativeMapActivity` trong Android manifest**

Thêm activity và metadata API key cho Google Maps Android.

- [ ] **Step 3: Tạo layout native cho map**

Layout phải có container cho `SupportMapFragment` hoặc `MapView`, và vùng action tối thiểu cho picker mode.

- [ ] **Step 4: Tạo `NativeMapActivity` với `OnMapReadyCallback`**

Phase đầu chỉ cần:
- render map
- nhận request từ intent
- set camera ban đầu
- show markers POI

- [ ] **Step 5: Build Android để khóa dependency + manifest**

Run:
```powershell
dotnet build "C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj" -f net9.0-android --no-restore
```

- [ ] **Step 6: Commit**

```bash
git add Mobile
git commit -m "feat: add native android map host"
```

### Task 3: Hoàn thiện browse flow cho `/mobile-map`

**Files:**
- Create: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\AndroidNativeMapService.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\MauiProgram.cs`

- [ ] **Step 1: Implement `AndroidNativeMapService` để mở browse map**

Service phải chuyển `MobileNativeMapRequest` thành intent và mở `NativeMapActivity`.

- [ ] **Step 2: Refactor `MobileMap.razor`**

Giữ phần tải POI/API hiện có, nhưng thay đoạn gọi `AppMapHelper.*` bằng native service khi chạy trên mobile.

- [ ] **Step 3: Giữ `focusId` contract**

`MobileMap.razor` vẫn nhận `focusId`, nhưng thay vì JS focus marker, request gửi vào native activity sẽ mang `FocusedPoiId`.

- [ ] **Step 4: Giữ directions flow từ `PoiDetail.razor`**

Nút “xem bản đồ lớn” trên mobile vẫn mở `/mobile-map?focusId=...`, nhưng đích cuối sẽ là native map.

- [ ] **Step 5: Manual smoke test browse flow**

Kiểm tra:
- mở `/mobile-map`
- có marker POI
- current location/recenter chạy
- mở từ `focusId` đúng POI
- directions mở ra Google Maps

- [ ] **Step 6: Commit**

```bash
git add SharedUI Mobile
git commit -m "feat: route mobile browse map to native android map"
```

### Task 4: Hoàn thiện picker tọa độ cho owner/admin trên mobile

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\NativeMapActivity.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\AndroidNativeMapService.cs`

- [ ] **Step 1: Thêm picker mode vào request/result**

Picker phải trả về `lat/lng`, và có nút xác nhận chọn vị trí.

- [ ] **Step 2: Nối `MyStore.razor` sang native picker khi chạy mobile**

Giữ nguyên form hiện tại, chỉ thay cơ chế chọn map.

- [ ] **Step 3: Nối `Locations.razor` sang native picker khi chạy mobile**

Flow admin và owner phải thống nhất.

- [ ] **Step 4: Manual smoke test picker**

Kiểm tra:
- mở picker từ owner
- chọn tọa độ
- form nhận lại `lat/lng`
- save POI không mất dữ liệu

- [ ] **Step 5: Commit**

```bash
git add SharedUI Mobile
git commit -m "feat: add native android picker for owner and admin"
```

### Task 5: Tách mobile khỏi JS map dependency ở các điểm cốt lõi

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\index.html`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Mobile\wwwroot\runtime-config.js`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`

- [ ] **Step 1: Gỡ phụ thuộc không còn cần cho mobile map**

Không xóa JS helper dùng chung cho web, nhưng mobile browse/picker không được phụ thuộc vào browser key nữa.

- [ ] **Step 2: Giữ runtime config chỉ cho các phần web hoặc fallback khác**

Tài liệu hóa rõ: `runtime-config.js` không còn là nguồn map key cho native Android flow.

- [ ] **Step 3: Chạy lại build toàn bộ các project bị ảnh hưởng**

Run:
```powershell
dotnet build "C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\FoodStreet.Server.csproj" --no-restore
dotnet build "C:\Users\letro\source\repos\PROJECT C#\SharedUI\FoodStreet.UI.csproj" --no-restore
dotnet build "C:\Users\letro\source\repos\PROJECT C#\Frontend\FoodStreet.Client.csproj" --no-restore
dotnet build "C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj" -f net9.0-android --no-restore
```

- [ ] **Step 4: Commit**

```bash
git add SharedUI Mobile Frontend
git commit -m "refactor: decouple mobile map flow from js runtime config"
```

### Task 6: Tài liệu hóa cấu hình và checklist test

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\README.md`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\GOOGLE_CLOUD_ADC_SETUP.md`
- Create: `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\specs\2026-04-01-mobile-android-native-map-design.md`

- [ ] **Step 1: Thêm hướng dẫn cấu hình key Android native**

Nêu rõ:
- package name
- SHA-1
- API cần bật
- nơi đặt key local

- [ ] **Step 2: Viết checklist smoke test**

Checklist tối thiểu:
- browse map
- focus POI
- recenter
- directions
- owner/admin picker

- [ ] **Step 3: Commit**

```bash
git add README.md GOOGLE_CLOUD_ADC_SETUP.md docs
git commit -m "docs: add android native map setup and smoke checklist"
```

## Rủi ro Chính

- Binding package Google Maps Android có thể cần tinh chỉnh để tương thích MAUI/AndroidX hiện tại.
- Nếu cố kéo `Places autocomplete` vào phase đầu, độ phức tạp tăng mạnh; không nên trộn sớm.
- Secret management cho Android API key phải giữ ngoài source of truth production.

## Definition of Done

- Mobile Android không còn phụ thuộc JS map để browse/picker.
- `/mobile-map` mở map native, render POI, recenter được.
- `focusId` trên mobile vẫn hoạt động.
- Owner/admin chọn tọa độ trên mobile bằng native picker.
- Web map không bị ảnh hưởng.
- `SharedUI`, `Frontend`, `Mobile`, `FoodStreet.Server` build sạch.
