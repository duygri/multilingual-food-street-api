# Vinh Khanh Master Roadmap Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Hoàn thiện hệ thống thuyết minh đa ngôn ngữ Vĩnh Khánh theo hướng `C# / ASP.NET Core / PostgreSQL / SignalR / Google Cloud / Google Maps`, với web ổn định và mobile Android chuyển dần sang native map.

**Architecture:** Backend tiếp tục là trung tâm cho auth, POI content, translation, TTS, QR, tour, analytics và moderation. Web giữ `Google Maps JavaScript API`. Mobile Android tách dần khỏi JS map để dùng `Maps SDK for Android` theo chiến lược core-first. Google Cloud cho `Translate + TTS` được chuẩn hóa theo service account/ADC, không phụ thuộc đường tắt tạm thời.

**Tech Stack:** ASP.NET Core Web API, EF Core, PostgreSQL, ASP.NET Identity/JWT, SignalR, Blazor WebAssembly, .NET MAUI Blazor Hybrid, Google Cloud Translation, Google Cloud Text-to-Speech, Google Maps JavaScript API, Google Maps SDK for Android.

---

## Mốc tài liệu hiện có

- PRD/UML chuẩn:
  - `C:\Users\letro\source\repos\PROJECT C#\PRD_UML_VinhKhanh_CSharp.html`
- Plan tổng cũ:
  - `C:\Users\letro\source\repos\PROJECT C#\CODE_PLAN_VinhKhanh.md`
- Spec mobile native map:
  - `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\specs\2026-04-01-mobile-android-native-map-design.md`
- Plan chi tiết mobile native map:
  - `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\plans\2026-04-01-mobile-android-native-map-core-first.md`

## Thứ tự ưu tiên đề xuất

1. Ổn định `Google Cloud auth + TTS/Translate`
2. Chốt lại `web map` và các flow JS map còn chạy trên browser
3. Hoàn thành `mobile Android native map phase 1`
4. Làm tiếp `mobile native advanced` (`Places`, `geofence`)
5. Polish `QA + docs + smoke checklist`

## Status snapshot 2026-04-02

- `Task 1` đến `Task 7` đã hoàn thành về mặt code và build.
- Build gần nhất:
  - `FoodStreet.Server`: `0 warning / 0 error`
  - `SharedUI`: `0 warning / 0 error`
  - `Frontend`: `0 warning / 0 error`
  - `Mobile (net9.0-android)`: `0 warning / 0 error`
- Runtime smoke đã xác nhận:
  - `GET /api/adminaudio/status` trả `200 OK`
  - `POST /api/adminaudio/health-check` trả `200 OK`
  - `POST /api/content/auth/login` trả token hợp lệ cho admin seed
  - `GET /api/content/auth/me` và `GET /api/content/auth/debug/claims` trả đúng identity/role
  - `GET /api/qrcode`, `GET /api/qrcode/{id}/meta`, `GET /api/qrcode/{id}` hoạt động
  - `GET /api/maps/locations/{id}` và `GET /api/maps/locations/near` hoạt động
  - backend local không còn crash bởi `Windows EventLog`
  - backend local không còn warning `DataProtection` trong môi trường sandbox hiện tại
- Hạng mục còn lại chủ yếu là `manual smoke test` trên browser/emulator/device và xác nhận Google Cloud ở môi trường có outbound internet thật.

## File Structure Theo Cụm Việc

### Backend Google Cloud / Auth

- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Configuration\GoogleCloudOptions.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\GoogleCloud\GoogleCloudAccessTokenProvider.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\Audio\GoogleTranslator.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\Audio\GoogleTtsService.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Infrastructure\StartupValidator.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Controllers\AuthController.cs`
- `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\appsettings.Development.json`

### Web Map

- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js`
- `C:\Users\letro\source\repos\PROJECT C#\Frontend\wwwroot\runtime-config.js`

### Mobile Native Map

- `C:\Users\letro\source\repos\PROJECT C#\Mobile\FoodStreet.Mobile.csproj`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\MauiProgram.cs`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\AndroidManifest.xml`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\*`
- `C:\Users\letro\source\repos\PROJECT C#\Mobile\Resources\layout\*`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`

### Docs / Ops

- `C:\Users\letro\source\repos\PROJECT C#\README.md`
- `C:\Users\letro\source\repos\PROJECT C#\GOOGLE_CLOUD_ADC_SETUP.md`
- `C:\Users\letro\source\repos\PROJECT C#\CODE_PLAN_VinhKhanh.md`

## Task 1: Chuẩn hóa Google Cloud credential flow

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Configuration\GoogleCloudOptions.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\GoogleCloud\GoogleCloudAccessTokenProvider.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\Audio\GoogleTranslator.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Services\Audio\GoogleTtsService.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Infrastructure\StartupValidator.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\appsettings.Development.json`

- [ ] **Step 1: Thêm chế độ credential rõ ràng**

Chuẩn hóa options:
- `ApiKey`
- `ProjectId`
- `CliPath`
- `UseServiceAccountJson`
- `CredentialPath`

- [ ] **Step 2: Cho provider đọc `GOOGLE_APPLICATION_CREDENTIALS` thật sự**

Không chỉ gọi `gcloud`; nếu có JSON path thì ưu tiên dùng `GoogleCredential`/ADC đúng chuẩn.

- [ ] **Step 3: Cho `GoogleTranslator` và `GoogleTtsService` log rõ auth mode**

Phân biệt:
- API key
- gcloud/ADC
- service account JSON

- [ ] **Step 4: Làm startup warning rõ ràng hơn**

`StartupValidator` phải báo:
- thiếu project id
- thiếu credential path
- đang chạy mode nào

- [ ] **Step 5: Build backend**

Run:
```powershell
dotnet build "C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\FoodStreet.Server.csproj" --no-restore
```

- [ ] **Step 6: Commit**

```bash
git add FoodStreet.Server
git commit -m "feat: support google cloud service account credential flow"
```

## Task 2: Chốt hợp đồng auth còn dang dở

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Controllers\AuthController.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\DTOs\AuthDtos.cs`
- Test/Verify: auth login + refresh flow

- [ ] **Step 1: Chọn một hướng duy nhất cho refresh token**

Chọn 1 trong 2:
- bỏ contract refresh cho sạch
- hoặc implement đầy đủ lưu/rotate/validate

- [ ] **Step 2: Đồng bộ login response với behavior thực**

Không để login trả field mà backend không xử lý được.

- [ ] **Step 3: Build server và smoke test auth**

Kiểm tra:
- login
- refresh hoặc không còn refresh endpoint gây nhiễu

- [ ] **Step 4: Commit**

```bash
git add FoodStreet.Server
git commit -m "fix: align auth token contract with backend behavior"
```

## Task 3: Ổn định web map làm baseline

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\Frontend\wwwroot\runtime-config.js`

- [ ] **Step 1: Chốt browser key và warning messages**

Đảm bảo web chạy được với `Maps JavaScript API + Places + Static Maps`.

- [ ] **Step 2: Smoke test 4 flow web**

Kiểm tra:
- `/map`
- focus từ `PoiDetail`
- picker trong `Locations`
- picker trong `MyStore`

- [ ] **Step 3: Build web**

Run:
```powershell
dotnet build "C:\Users\letro\source\repos\PROJECT C#\SharedUI\FoodStreet.UI.csproj" --no-restore
dotnet build "C:\Users\letro\source\repos\PROJECT C#\Frontend\FoodStreet.Client.csproj" --no-restore
```

- [ ] **Step 4: Commit**

```bash
git add SharedUI Frontend
git commit -m "fix: stabilize web google maps baseline"
```

## Task 4: Thực thi mobile Android native map phase 1

**Files:**
- Use plan: `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\plans\2026-04-01-mobile-android-native-map-core-first.md`

- [ ] **Step 1: Thực thi Task 1 trong plan mobile native map**
- [ ] **Step 2: Thực thi Task 2 trong plan mobile native map**
- [ ] **Step 3: Thực thi Task 3 trong plan mobile native map**
- [ ] **Step 4: Thực thi Task 4 trong plan mobile native map**
- [ ] **Step 5: Thực thi Task 5 trong plan mobile native map**
- [ ] **Step 6: Thực thi Task 6 trong plan mobile native map**

## Task 5: Mobile native advanced

**Files:**
- Modify/Create dưới:
  - `C:\Users\letro\source\repos\PROJECT C#\Mobile\Platforms\Android\Maps\*`
  - `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\GeofenceTest.razor`
  - `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor`
  - `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor`

- [ ] **Step 1: Thêm Places autocomplete native cho picker**
- [ ] **Step 2: Thêm geofence circle overlay cho mobile native map**
- [ ] **Step 3: Quyết định có native hóa `GeofenceTest` hay giữ web/admin-only**
- [ ] **Step 4: Build mobile + smoke test**

- [ ] **Step 5: Commit**

```bash
git add SharedUI Mobile
git commit -m "feat: add native android places and geofence overlays"
```

## Task 6: TTS/Translate verification UX

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\TtsConsole.razor`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\FoodStreet.Server\Controllers\AdminAudioController.cs`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\GOOGLE_CLOUD_ADC_SETUP.md`

- [ ] **Step 1: Hiển thị rõ credential mode đang dùng trong TTS Console**
- [ ] **Step 2: Thêm quick health check cho Translate/TTS**
- [ ] **Step 3: Viết checklist test service account / API key / ADC**
- [ ] **Step 4: Build server + UI**

- [ ] **Step 5: Commit**

```bash
git add FoodStreet.Server SharedUI GOOGLE_CLOUD_ADC_SETUP.md
git commit -m "feat: improve google cloud diagnostics and tts verification"
```

## Task 7: Docs và handoff

**Files:**
- Modify: `C:\Users\letro\source\repos\PROJECT C#\README.md`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\CODE_PLAN_VinhKhanh.md`
- Modify: `C:\Users\letro\source\repos\PROJECT C#\GOOGLE_CLOUD_ADC_SETUP.md`

- [ ] **Step 1: Cập nhật README với map strategy mới**

Nêu rõ:
- web dùng JS map
- mobile Android dùng native map

- [ ] **Step 2: Cập nhật `CODE_PLAN_VinhKhanh.md` thành bản summary**

File root chỉ nên đóng vai trò overview và trỏ tới master roadmap này.

- [ ] **Step 3: Viết checklist smoke test cuối**

Checklist tối thiểu:
- auth
- translate
- tts
- web map
- mobile native map
- owner picker

- [ ] **Step 4: Commit**

```bash
git add README.md CODE_PLAN_VinhKhanh.md GOOGLE_CLOUD_ADC_SETUP.md docs
git commit -m "docs: align project roadmap with google cloud and native mobile map"
```

## Rủi ro Chính

- `GoogleCloudAccessTokenProvider` hiện vẫn nghiêng về `gcloud`; nếu không sửa sớm sẽ làm service account JSON trở nên “có mà như không”.
- Mobile native map có rủi ro dependency/binding package với `net9.0-android`, nên phải build khóa từng task.
- Nếu cố trộn `Places` + `geofence` vào phase đầu native map, tiến độ sẽ chậm đáng kể.
- Key management hiện tách thành `web browser key` và `android app key`; docs phải nói rất rõ để tránh dùng nhầm.

## Definition of Done

- Backend dùng được `Google Cloud` theo một auth mode rõ ràng, test được.
- Auth contract không còn endpoint/field gây hiểu lầm.
- Web map ổn định làm baseline.
- Mobile Android có native map cho browse + picker.
- Docs đủ rõ để team tiếp tục chạy sau mà không cần ráp lại context từ chat.
