# Thiết Kế Chuyển Mobile Map Sang Native Android (Core-First)

## Bối cảnh

Hiện tại web và mobile đang dùng chung lớp bản đồ JavaScript thông qua [SharedUI/Pages/Map.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor), [SharedUI/Pages/MobileMap.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor), và [SharedUI/wwwroot/js/google-maps-helper.js](C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js). Trên web, hướng này chấp nhận được vì nó đúng mô hình `Maps JavaScript API`. Trên mobile MAUI Blazor Hybrid, toàn bộ map vẫn chạy bên trong WebView, nên key restriction kiểu website/referrer dễ không ổn định và debugging khó hơn.

Google khuyến nghị với ứng dụng mobile nên ưu tiên SDK native thay vì WebView map. Tài liệu security của Google cũng lưu ý rằng ứng dụng hybrid tải tài nguyên cục bộ trong WebView có thể không gửi `Referer` theo cách mà website-restricted key mong đợi. Đây là lý do thiết kế mới sẽ giữ `web = JavaScript`, nhưng chuyển `mobile Android = Maps SDK for Android`.

## Mục tiêu

- Chuyển toàn bộ luồng map trên Android mobile sang `Maps SDK for Android`.
- Giữ nguyên web map hiện tại để tránh làm vỡ phần browser.
- Tách rõ trách nhiệm: `SharedUI` giữ flow nghiệp vụ, `Mobile` chịu trách nhiệm render native map.
- Ưu tiên một phase đầu chạy ổn cho các luồng cốt lõi trước khi làm autocomplete/geofence native.

## Ngoài phạm vi của phase đầu

- Chưa native hóa `Places autocomplete`.
- Chưa native hóa `geofence overlay/test`.
- Chưa thay đổi web map.
- Chưa làm iOS native map.

## Quyết định kiến trúc

### Phương án được chọn: Core-first, mobile native riêng

- `Web` tiếp tục dùng [SharedUI/Pages/Map.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor) và [SharedUI/wwwroot/js/google-maps-helper.js](C:\Users\letro\source\repos\PROJECT C#\SharedUI\wwwroot\js\google-maps-helper.js).
- `Mobile Android` sẽ mở một màn hình native riêng để hiển thị map, marker, current location, focus POI, recenter, và picker tọa độ.
- `SharedUI` chỉ gọi qua abstraction `IMobileNativeMapService`, không biết chi tiết native map được render như thế nào.

### Vì sao không chọn “đổi sạch một lượt”

Đổi toàn bộ `browse + picker + places + geofence` trong một đợt làm tăng rủi ro gãy nhiều flow khó debug cùng lúc. Kiến trúc hiện tại đã có logic nghiệp vụ ổn ở Blazor; vì vậy chiến lược an toàn hơn là native hóa phần map cốt lõi trước, giữ nguyên API và DTO hiện có, sau đó nối tiếp autocomplete/geofence ở phase sau.

## Thiết kế module

### 1. SharedUI

SharedUI sẽ giữ route, dữ liệu, và flow nghiệp vụ:

- [SharedUI/Pages/MobileMap.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor)
- [SharedUI/Pages/Admin/MyStore.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor)
- [SharedUI/Pages/Admin/Locations.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor)

SharedUI sẽ thêm:

- `IMobileNativeMapService`
- DTO request/response cho native map:
  - browse mode
  - picker mode
  - focused POI
  - user location
  - selected coordinate result

Nguyên tắc là page Blazor không còn gọi `AppMapHelper` khi đang chạy trên mobile Android.

### 2. Mobile

Project [Mobile](C:\Users\letro\source\repos\PROJECT C#\Mobile) sẽ thêm các thành phần native Android:

- `AndroidNativeMapService`: bridge giữa Blazor và Android activity.
- `NativeMapActivity`: màn hình native map.
- layout XML cho native map.
- parser/serializer cho request và result.

`NativeMapActivity` sẽ dùng `SupportMapFragment` hoặc `MapView` theo hướng native của `Maps SDK for Android`. Ở phase đầu, activity sẽ đảm nhiệm:

- khởi tạo map
- render marker POI
- marker current location
- focus camera
- recenter
- chọn vị trí trong picker mode
- trả kết quả về Blazor page
- mở directions ra Google Maps app hoặc URL ngoài

## Data Flow

### A. Browse map trên mobile

1. Người dùng vào `/mobile-map`.
2. Page Blazor tải dữ liệu POI qua API hiện có.
3. Page dựng `MobileNativeMapRequest`.
4. `IMobileNativeMapService` mở `NativeMapActivity`.
5. Activity render map native, marker, current location, focus POI nếu có `focusId`.
6. Khi đóng activity, page vẫn giữ flow điều hướng và dữ liệu như hiện tại.

### B. Picker tọa độ cho owner/admin trên mobile

1. Người dùng mở form tạo/sửa POI.
2. Page gọi `IMobileNativeMapService.OpenPickerAsync(...)`.
3. Activity vào picker mode.
4. Người dùng chọn vị trí trên map native.
5. Activity trả lại `lat/lng`.
6. Page cập nhật form POI và gọi API save như cũ.

## Xử lý khóa API

- Web tiếp tục dùng key `Maps JavaScript API`/`Places`/`Maps Static API`.
- Android native dùng key riêng, application restriction kiểu `Android apps`.
- Key Android sẽ gắn với package `com.companyname.foodstreet.mobile` và SHA-1 của app.

Trong phase đầu, cấu hình native key phải nằm ở phía Android app, không dùng lại `runtime-config.js` của WebView. Đây là chủ đích, để tách hoàn toàn mobile native khỏi browser key.

## Xử lý lỗi và fallback

- Nếu không có quyền vị trí: map vẫn mở ở tọa độ mặc định Vĩnh Khánh.
- Nếu lỗi Google Play Services hoặc key Android sai: hiện thông báo native rõ ràng, không để màn trắng.
- Nếu picker trả kết quả lỗi: form POI giữ nguyên dữ liệu cũ.
- Nếu page đang chạy trên web: tiếp tục dùng JS map, không đi vào native branch.

## Tương thích với code hiện tại

Thiết kế mới phải giữ các contract nghiệp vụ đã có:

- API lấy POI gần:
  - [SharedUI/Pages/MobileMap.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\MobileMap.razor)
  - [SharedUI/Pages/Map.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Map.razor)
- Route chi tiết mở map lớn bằng `focusId`:
  - [SharedUI/Pages/PoiDetail.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\PoiDetail.razor)
- Form tạo/sửa POI trong owner/admin:
  - [SharedUI/Pages/Admin/MyStore.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\MyStore.razor)
  - [SharedUI/Pages/Admin/Locations.razor](C:\Users\letro\source\repos\PROJECT C#\SharedUI\Pages\Admin\Locations.razor)

Nghĩa là phase đầu không đổi domain model, không đổi backend map API, chỉ đổi renderer và map interaction trên Android mobile.

## Kế hoạch phase

### Phase 1: Core native map

- Native browse map cho `/mobile-map`
- Focus POI bằng `focusId`
- Current location + recenter
- Marker POI + info cơ bản
- Directions
- Picker tọa độ cho owner/admin trên mobile

### Phase 2: Native nâng cao

- Places autocomplete native
- Geofence overlay native
- `GeofenceTest` giữ vai trò admin/browser-only, không native hóa trong phase này
- Camera/marker polish
- Tinh chỉnh UX cho owner/admin

## Kiểm thử

Do repo hiện chưa có Android UI test harness riêng, phase đầu sẽ xác minh bằng:

- build `SharedUI`
- build `Mobile` Android
- smoke test trên emulator/device:
  - mở `/mobile-map`
  - mở POI bằng `focusId`
  - recenter
  - directions
  - picker tọa độ trong owner/admin

## Kết luận

Thiết kế này tách rõ `web map` và `mobile map` theo đúng bản chất platform, giảm phụ thuộc vào WebView key restriction, và giữ cho hệ thống hiện tại thay đổi vừa đủ để kiểm soát rủi ro. Đây là nền phù hợp để làm tiếp autocomplete/geofence native mà không phải đập lại phần map một lần nữa.
