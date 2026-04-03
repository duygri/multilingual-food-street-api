# 🍜 Food Street System

![version](https://img.shields.io/badge/version-1.0.0-blue) ![build](https://img.shields.io/badge/build-passing-success)

Hệ thống quản lý và hướng dẫn du lịch ẩm thực Phố Vĩnh Khánh (Quận 4, TP.HCM), cung cấp giải pháp chuyển đổi số toàn diện cho trải nghiệm ẩm thực đường phố. Vui lòng xem [Tài liệu Server](FoodStreet.Server/README.md) để biết thêm chi tiết về các tính năng Backend.

## Các dự án thành phần (Packages)

Hệ thống được thiết kế theo cấu trúc Modular và gồm ba dự án (packages) chính:

- [**FoodStreet.Server**](./FoodStreet.Server/) ![version](https://img.shields.io/badge/version-1.0.0-blue) - Server Backend API và dịch vụ xác thực. Đây là dự án lõi xử lý dữ liệu, dành cho các truy vấn HTTP REST API, kết nối cơ sở dữ liệu PostgreSQL. Phù hợp làm nền tảng cho mọi Client.
- [**FoodStreet.Client**](./FoodStreet.Client/) ![version](https://img.shields.io/badge/version-1.0.0-blue) - Frontend dành cho Quản trị viên (Admin) và Đối tác kinh doanh (Seller), chạy trên trình duyệt sử dụng Blazor WebAssembly.
- [**SharedUI (FoodStreet.UI)**](./SharedUI/) ![version](https://img.shields.io/badge/version-1.0.0-blue) - Thư viện giao diện (Razor Class Library). Chứa logic và UI dùng chung để tiết kiệm code khi phát triển cả nền tảng Web lẫn thiết bị di động (Mobile App - MAUI Blazor Hybrid).

## Bắt đầu nhanh (Getting Started)

Để bắt đầu, hãy làm theo hướng dẫn trong phần Tài liệu Cấu hình Dữ liệu bên dưới, với các lệnh trực tiếp để cài đặt.

Bạn cũng có thể xem qua mã nguồn tại thư mục [SharedUI](./SharedUI/) và các controller trong [Tài liệu API](./FoodStreet.Server/) để kiểm tra tiến trình hoạt động.

### Yêu cầu cấu hình

- .NET 8 SDK
- PostgreSQL và pgAdmin 4
- Visual Studio 2022 (khuyến nghị)
- Google Cloud project cho `Translate + TTS`
- Google Maps Platform project cho web/mobile map

### Thiết lập CSDL và Chạy

1. Tạo Database `FoodStreetDB` trên PostgreSQL (qua pgAdmin).
2. Cập nhật chuỗi kết nối trong `FoodStreet.Server/appsettings.json`:
   ```json
   "DefaultConnection": "Host=localhost;Port=5432;Database=FoodStreetDB;Username=postgres;Password=YOUR_PASSWORD"
   ```
3. Chạy lần lượt các lệnh sau trên terminal:
   ```bash
   dotnet restore
   dotnet ef database update --project FoodStreet.Server
   dotnet run --project FoodStreet.Server
   ```

## Google Cloud và Google Maps

### Backend `Translate + TTS`

`FoodStreet.Server` hiện hỗ trợ 3 cách xác thực Google Cloud:

- `Service account JSON` qua `GOOGLE_APPLICATION_CREDENTIALS` hoặc `GoogleCloud:CredentialPath`
- `ADC / gcloud`
- `API key` cho trường hợp local/dev cần đi nhanh

Tài liệu chi tiết:

- [Google Cloud setup](./GOOGLE_CLOUD_ADC_SETUP.md)

### Web map

Web vẫn dùng `Google Maps JavaScript API` qua browser key tại:

- [Frontend runtime-config](./Frontend/wwwroot/runtime-config.js)

Cần bật:

- `Maps JavaScript API`
- `Places API`
- `Maps Static API`

### Mobile Android native map

Mobile Android đã chuyển flow `browse map + focus + directions + picker tọa độ` sang native `Maps SDK for Android`.
Autocomplete tìm địa điểm và vòng geofence overlay cho POI cũng đã chạy trên native map.

Key Android native không đọc từ `runtime-config.js`. Thay vào đó:

1. Copy file:
   - `Mobile/Resources/values/google_maps_api.xml.template`
2. Tạo file local:
   - `Mobile/Resources/values/google_maps_api.xml`
3. Thay `__SET_ANDROID_MAPS_KEY__` bằng Android-restricted key

Cần bật:

- `Maps SDK for Android`

Application restriction khuyến nghị:

- `Android apps`
- package name hiện tại: `com.companyname.foodstreet.mobile`
- thêm `SHA-1` của debug/release keystore tương ứng

Lưu ý:

- `Mobile/wwwroot/runtime-config.js` giờ chỉ còn dùng cho các màn fallback WebView như JS/Static Maps còn sót
- flow native browse/picker không còn phụ thuộc browser key nữa

## Smoke Checklist

### Backend / Google Cloud

- login nhận access token và dùng được trên admin pages
- `GET /api/content/auth/me` và `GET /api/content/auth/debug/claims` trả đúng identity/role sau login
- `GET /api/adminaudio/status` trả đúng `AuthMode`, `ProjectId`, `CredentialFile`
- `POST /api/adminaudio/health-check` pass cho cả `Translate` và `TTS`
- `/admin/tts-console` chạy được `Quick TTS Test`
- `GET /api/qrcode`, `GET /api/qrcode/{id}/meta`, `GET /api/qrcode/{id}` trả dữ liệu/PNG đúng cho POI public
- `GET /api/maps/locations/{id}` và `GET /api/maps/locations/near` trả được POI public

### Web

- `/map` hiển thị POI và focus từ trang chi tiết
- modal `Locations` chọn tọa độ bằng JS picker
- modal `MyStore` chọn tọa độ bằng JS picker

### Mobile Android

- `/mobile-map` mở native map
- focus từ `PoiDetail` nhảy đúng POI
- nút recenter hoạt động
- search địa điểm trong native picker hoạt động
- POI có geofence hiển thị vòng bán kính trên native map
- bấm info window mở walking directions
- modal `MyStore` mở native picker và nhận lại `lat/lng`
- modal `Locations` mở native picker và nhận lại `lat/lng`

### Geofence debug

- `/admin/geofence-test` được giữ như công cụ debug admin trên trình duyệt
- mobile Android dùng geofence overlay trực tiếp trong native map, không nhân đôi trang test này

### Tour

- `GET /api/tours` cần có ít nhất 1 tour `IsActive = true` để smoke test runtime end-to-end
- nếu danh sách tour đang rỗng, tạo trước một tour active trong admin rồi mới test `/tours` và flow `start / resume / progress`

## Về Dự án Food Street (About Project)

Dự án Phố Ẩm Thực Vĩnh Khánh là một sản phẩm thực tế nhằm chuẩn hóa việc khám phá văn hóa ẩm thực địa phương (POI - Point of Interest). Nó cung cấp cho các ứng dụng cơ chế an toàn để thu thập vị trí GPS, thông báo tự động cho khách du lịch trong bán kính (Geofencing) và thay đổi ngôn ngữ động cho mọi đối tượng.

Để biết thêm thông tin về dự án:

- [Tài liệu Kiến trúc hệ thống](#)
- [Cơ chế Thuyết minh giọng nói (TTS)](#)
- [Đặc tả GPS và Geofencing](#)
- [Trang GitHub của Tổ chức](https://github.com/duygri)

## Bản quyền và Giấy phép (License)

Dự án này được bảo vệ bản quyền và cấp phép theo [Internal Project License](#).
