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
- **Cơ sở dữ liệu**: PostgreSQL


## 📦 Cấu trúc Solution
- `FoodStreet.Server`: Backend API & Auth Server.
- `FoodStreet.Client`: Frontend Client (Web).
- `FoodStreet.UI`: Thư viện giao diện dùng chung (Shared UI).

## 🔧 Cài đặt & Chạy (Local Development)

### 1. Yêu cầu
- .NET 8 SDK
- PostgreSQL & pgAdmin 4

### 2. Cấu hình Database
1.  Mở **pgAdmin**, tạo một Database mới tên là `FoodStreetDB`.
2.  Mở file `FoodStreet.Server/appsettings.json` (và `appsettings.Development.json`).
3.  Cập nhật dòng `DefaultConnection` với mật khẩu PostgreSQL của bạn:
    ```json
    "DefaultConnection": "Host=localhost;Port=5432;Database=FoodStreetDB;Username=postgres;Password=YOUR_PASSWORD"
    ```

### 3. Cài đặt & Chạy
1.  Clone dự án:
    ```bash
    git clone https://github.com/duygri/multilingual-food-street-api.git
    cd multilingual-food-street-api
    ```
2.  Khôi phục packages:
    ```bash
    dotnet restore
    ```
3.  Cập nhật Database (Chạy lệnh này để tạo bảng):
    ```bash
    dotnet ef database update --project FoodStreet.Server
    ```
4.  Chạy dự án bằng Visual Studio (F5) hoặc lệnh:
    ```bash
    dotnet run --project FoodStreet.Server
    ```

## 📄 License
Internal Project.
