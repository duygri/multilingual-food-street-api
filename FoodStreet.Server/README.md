# ⚙️ FoodStreet Server Đã Đăng Cấp

![version](https://img.shields.io/badge/version-1.0.0-blue) ![framework](https://img.shields.io/badge/.NET-8.0-512BD4)

API Server chính thức cho hệ thống Phố Ẩm Thực Vĩnh Khánh, xây dựng bằng ASP.NET Core Web API (C#) để cung cấp dữ liệu và nền tảng dịch vụ. Vui lòng xem tài liệu [Kiến trúc Giải pháp](../README.md) để biết tổng quan về cách cài đặt và tương tác giữa các hệ thống. 

## Các phân hệ (Features/Modules)

Backend này tổ chức dữ liệu thành các phân hệ chính sau đây:

- [**Quản lý Địa điểm (POI)**](#) ![status](https://img.shields.io/badge/status-active-success) - Quản lý CRUD cho địa điểm, cửa hàng ăn uống với cấu trúc hỗ trợ đa ngôn ngữ linh hoạt (Tiếng Việt, Tiếng Anh). 
- [**Dịch vụ Định vị (GPS & Geofence)**](#) ![status](https://img.shields.io/badge/status-active-success) - Cung cấp tọa độ, tính toán gợi ý thông minh các món ăn gần vùng theo thời gian thực và kích hoạt thuyết minh thông qua kiểm tra bán kính chuẩn.
- [**Xử lý Đa phương tiện & TTS**](#) ![status](https://img.shields.io/badge/status-active-success) - Quản lý files thuyết minh giọng nói (Text-To-Speech) và hình ảnh được lưu trữ tĩnh trong thư mục `wwwroot/`.
- [**Xác thực Bảo mật**](#) ![status](https://img.shields.io/badge/status-active-success) - Lớp ủy quyền bằng JWT (JSON Web Tokens) và ASP.NET Identity đảm bảo kết nối an toàn với mọi Client (Web & Mobile).

## Bắt đầu nhanh (Getting Started)

Để khởi chạy server, tham khảo các bước lệnh dưới đây để chạy và truy cập Swagger.

Bạn cũng có thể xem qua thư mục [Controllers](./Controllers/) và thư mục cấu trúc [Mô hình dữ liệu (Models)](./Models/) để biết chi tiết trả về của từng API.

### Thiết lập Server

```bash
# Phục hồi các gói phụ thuộc
dotnet restore

# Chạy bản cập nhật schema lên PostgreSQL
dotnet ef database update --project FoodStreet.Server

# Tiến hành chạy Server
dotnet run --project FoodStreet.Server
```

Khi chạy thành công, API sẽ hoạt động tại địa chỉ mặc định: `https://localhost:7170`. Bạn có thể truy cập bằng trình duyệt `${URL}/swagger` để xem trực tiếp cấu trúc OpenAPI của toàn hệ thống.

## Về Tổ chức Dự án (About Backend)

Ứng dụng backend này được xây dựng tuân thủ mô hình Model-Controller chuẩn (MVC dành cho API) với các đặc tả và giao tiếp qua lớp Service trung gian (Logic nghiệp vụ). Lớp Entity Framework Core đảm nhiệm toàn bộ vai trò kết nối tới PostgreSQL một cách an toàn và tối ưu thời gian phản hồi.

Để biết thêm thông tin về các luồng xử lý Backend:

- [Tài liệu Thiết kế Cơ sở dữ liệu](#)
- [Cấu hình Phân quyền (Roles & Auth)](#)
- [Tài liệu REST API Tham khảo](#)

## Bản quyền và Giấy phép (License)

Dự án này là tài sản nội bộ và được cấp phép theo mô hình [Internal Closed-Source License](#).
