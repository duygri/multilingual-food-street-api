# Vĩnh Khánh Food Street (FoodStreet)

Hệ thống quản lý và hướng dẫn du lịch ẩm thực Phố Vĩnh Khánh (Quận 4).

## 🚀 Giới thiệu
Dự án cung cấp giải pháp chuyển đổi số cho phố ẩm thực, bao gồm:
- **Web Portal**: Dành cho Quản trị viên (Admin) và Đối tác (Seller).
- **Mobile App (Future)**: Dành cho Khách du lịch (User).
- **Tính năng chính**: Quản lý món ăn, Thuyết minh âm thanh (TTS), Bản đồ GPS, Đa ngôn ngữ.

## 🛠️ Công nghệ
- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Blazor WebAssembly
- **Cơ sở dữ liệu**: SQL Server (Entity Framework Core)

## 📦 Cấu trúc Solution
- `FoodStreet.Server`: Backend API & Auth Server.
- `FoodStreet.Client`: Frontend Client (Web).
- `FoodStreet.UI`: Thư viện giao diện dùng chung (Shared UI).

## 🔧 Cài đặt & Chạy
1.  **Yêu cầu**: .NET 8 SDK, SQL Server.
2.  **Cài đặt**:
    ```bash
    git clone ...
    dotnet restore
    ```
3.  **Cập nhật Database**:
    ```powershell
    Update-Database
    ```
4.  **Chạy dự án**: Set Startup Projects là `FoodStreet.Server` và `FoodStreet.Client` (Multiple Startup).

## 📄 License
Internal Project.
