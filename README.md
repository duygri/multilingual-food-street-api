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

## Về Dự án Food Street (About Project)

Dự án Phố Ẩm Thực Vĩnh Khánh là một sản phẩm thực tế nhằm chuẩn hóa việc khám phá văn hóa ẩm thực địa phương (POI - Point of Interest). Nó cung cấp cho các ứng dụng cơ chế an toàn để thu thập vị trí GPS, thông báo tự động cho khách du lịch trong bán kính (Geofencing) và thay đổi ngôn ngữ động cho mọi đối tượng.

Để biết thêm thông tin về dự án:

- [Tài liệu Kiến trúc hệ thống](#)
- [Cơ chế Thuyết minh giọng nói (TTS)](#)
- [Đặc tả GPS và Geofencing](#)
- [Trang GitHub của Tổ chức](https://github.com/duygri)

## Bản quyền và Giấy phép (License)

Dự án này được bảo vệ bản quyền và cấp phép theo [Internal Project License](#).
