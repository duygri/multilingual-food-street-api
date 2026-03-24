# Vinh Khanh Food Street

He thong quan ly va huong dan du lich am thuc Pho Vinh Khanh (Quan 4, TP.HCM).

## Gioi thieu

Du an cung cap giai phap chuyen doi so cho pho am thuc, bao gom:

- **Web Portal**: Danh cho Quan tri vien (Admin) va Doi tac (Seller).
- **Mobile App**: Danh cho Khach du lich (User), ho tro GPS va thuyet minh tu dong.
- **Tinh nang chinh**: Quan ly dia diem (POI), Thuyet minh am thanh (TTS), Ban do GPS, Geofencing, Da ngon ngu.

## Cong nghe

- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Blazor WebAssembly
- **Mobile**: .NET MAUI Blazor Hybrid
- **Co so du lieu**: PostgreSQL
- **Shared UI**: Razor Class Library

## Cau truc Solution

- `FoodStreet.Server` - Backend API va Auth Server.
- `FoodStreet.Client` - Frontend Client (Blazor WebAssembly).
- `SharedUI` (FoodStreet.UI) - Thu vien giao dien dung chung.

## Cai dat va Chay

### Yeu cau

- .NET 8 SDK
- PostgreSQL va pgAdmin 4
- Visual Studio 2022 (khuyen nghi)

### Cau hinh Database

1. Mo pgAdmin, tao Database moi ten la `FoodStreetDB`.
2. Mo file `FoodStreet.Server/appsettings.json`.
3. Cap nhat dong `DefaultConnection` voi mat khau PostgreSQL cua ban:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=FoodStreetDB;Username=postgres;Password=YOUR_PASSWORD"
```

### Chay du an

1. Clone du an:

```bash
git clone https://github.com/duygri/multilingual-food-street-api.git
cd multilingual-food-street-api
```

2. Khoi phuc packages:

```bash
dotnet restore
```

3. Cap nhat Database:

```bash
dotnet ef database update --project FoodStreet.Server
```

4. Chay du an:

```bash
dotnet run --project FoodStreet.Server
```

Hoac mo bang Visual Studio va nhan F5.

## License

Internal Project.
