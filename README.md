# Sài Gòn Kể / Saigon Storyteller

> **English** — Expanding multilingual audio walking tours across Ho Chi Minh City, beginning with the stories, places, and food culture represented in this repository.
>
> **Tiếng Việt** — Mở rộng các chuyến tham quan đi bộ bằng âm thanh đa ngôn ngữ khắp Thành phố Hồ Chí Minh, bắt đầu từ những câu chuyện, địa điểm và văn hóa ẩm thực được thể hiện trong kho mã nguồn này.

## Current scope / Phạm vi hiện tại

**English** — The citywide frontend direction is in progress. Analytics bounds and some seeded or portal content remain scoped to District 4/Vĩnh Khánh. This affects how dashboards and data should be interpreted; it does **not** mean every visitor flow is restricted to one district.

**Tiếng Việt** — Định hướng giao diện toàn thành phố đang được triển khai. Ranh giới phân tích và một phần nội dung mẫu hoặc nội dung trên cổng thông tin vẫn thuộc phạm vi Quận 4/Vĩnh Khánh. Điều này ảnh hưởng đến cách diễn giải bảng điều khiển và dữ liệu; điều đó **không** có nghĩa mọi luồng trải nghiệm của khách truy cập đều bị giới hạn trong một quận.

## Runtime surfaces / Các bề mặt chạy ứng dụng

**English**

- ASP.NET Core API/server for application services, data, integrations, and real-time notifications.
- Blazor Web owner/admin portal for operational content and analytics work.
- .NET MAUI Android visitor app for the on-street visitor experience.

**Tiếng Việt**

- API/máy chủ ASP.NET Core cho các dịch vụ ứng dụng, dữ liệu, tích hợp và thông báo thời gian thực.
- Cổng chủ sở hữu/quản trị Blazor Web cho công việc vận hành nội dung và phân tích.
- Ứng dụng khách truy cập .NET MAUI Android cho trải nghiệm trên đường phố.

## What it supports / Các chức năng hỗ trợ

**English** — The repository supports POI discovery; multilingual audio and translation workflows; QR entry; maps and walking journeys; notifications; owner/admin operations; and analytics. These capabilities support the citywide direction, but the repository does not claim complete citywide content or coverage today.

**Tiếng Việt** — Kho mã nguồn hỗ trợ khám phá POI; quy trình âm thanh và dịch đa ngôn ngữ; truy cập qua QR; bản đồ và hành trình đi bộ; thông báo; vận hành cho chủ sở hữu/quản trị; và phân tích. Các chức năng này hỗ trợ định hướng toàn thành phố, nhưng kho mã nguồn hiện không tuyên bố đã có nội dung hoặc độ phủ hoàn chỉnh trên toàn thành phố.

## Repository map / Bản đồ kho mã nguồn

| Path / Đường dẫn | English | Tiếng Việt |
| --- | --- | --- |
| `src/` | Application projects: server, web portal, mobile app, and shared code. | Các dự án ứng dụng: máy chủ, cổng web, ứng dụng di động và mã dùng chung. |
| `tests/` | Server and web test projects. | Các dự án kiểm thử máy chủ và web. |
| `docs/` | Design, deployment, QR, and operational documentation. | Tài liệu thiết kế, triển khai, QR và vận hành. |
| `deploy/` | Deployment-related assets. | Tài nguyên liên quan đến triển khai. |
| `scripts/` | Operational and automation scripts. | Các tập lệnh vận hành và tự động hóa. |

## Prerequisites / Điều kiện tiên quyết

**English**

- The SDK is pinned by `global.json` to .NET SDK `9.0.312`.
- The server, web, and test projects target `net8.0`; the mobile app targets `net9.0-android35.0`.
- Running the API requires PostgreSQL. The normal test commands below do not.
- Android development requires the .NET MAUI Android workload, Android SDK platform 35, and an Android runtime/API level of 24 or newer. Use `dotnet workload list` to check the installed workloads; run `dotnet workload install maui-android` only when it is missing.

**Tiếng Việt**

- SDK được ghim trong `global.json` ở .NET SDK `9.0.312`.
- Các dự án máy chủ, web và kiểm thử nhắm tới `net8.0`; ứng dụng di động nhắm tới `net9.0-android35.0`.
- Chạy API cần PostgreSQL. Các lệnh kiểm thử thông thường bên dưới không cần PostgreSQL.
- Phát triển Android cần workload .NET MAUI Android, Android SDK platform 35 và Android runtime/API cấp 24 trở lên. Dùng `dotnet workload list` để kiểm tra các workload đã cài; chỉ chạy `dotnet workload install maui-android` khi workload này chưa có.

## Setup and validation / Thiết lập và kiểm tra

**English** — From the repository root, restore once, then build or test the required surface. The Android command builds a Debug package; it requires the MAUI Android workload and Android prerequisites above.

**Tiếng Việt** — Từ thư mục gốc của kho mã nguồn, khôi phục một lần, sau đó xây dựng hoặc kiểm thử bề mặt cần thiết. Lệnh Android xây dựng gói Debug; lệnh này cần workload MAUI Android và các điều kiện tiên quyết Android ở trên.

```powershell
dotnet restore NarrationApp.sln

dotnet workload list
# Only if maui-android is absent:
dotnet workload install maui-android

dotnet build src/NarrationApp.Server/NarrationApp.Server.csproj --no-restore
dotnet build src/NarrationApp.Web/NarrationApp.Web.csproj --no-restore

dotnet test tests/NarrationApp.Server.Tests/NarrationApp.Server.Tests.csproj --no-restore
dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore

dotnet build src/NarrationApp.Mobile/NarrationApp.Mobile.csproj -f net9.0-android35.0 -c Debug --no-restore
```

**English** — Server tests use EF Core InMemory and web tests are local component tests. PostgreSQL is required when running the real API, not for the standard test commands above.

**Tiếng Việt** — Kiểm thử máy chủ dùng EF Core InMemory và kiểm thử web là kiểm thử thành phần cục bộ. PostgreSQL cần khi chạy API thực, không cần cho các lệnh kiểm thử tiêu chuẩn ở trên.

## Run the API and portal / Chạy API và cổng thông tin

**English** — Start the API and the web portal in separate terminals. The API requires PostgreSQL to run; configure it before starting the server.

**Tiếng Việt** — Khởi động API và cổng web trong các terminal riêng biệt. API cần PostgreSQL để chạy; hãy cấu hình PostgreSQL trước khi khởi động máy chủ.

```powershell
dotnet run --project src/NarrationApp.Server/NarrationApp.Server.csproj --launch-profile https
dotnet run --project src/NarrationApp.Web/NarrationApp.Web.csproj --launch-profile https
```

## Local configuration / Cấu hình cục bộ

**English** — The web portal reads `src/NarrationApp.Web/wwwroot/appsettings.Development.json`; an optional, ignored `src/NarrationApp.Web/wwwroot/appsettings.Local.json` can provide local overrides. Configure server runtime values through user secrets or environment variables, including the PostgreSQL connection string, JWT settings, GoogleCloud, CloudflareR2, and Mapbox settings. Do not put real secrets in tracked configuration files.

**Tiếng Việt** — Cổng web đọc `src/NarrationApp.Web/wwwroot/appsettings.Development.json`; tệp `src/NarrationApp.Web/wwwroot/appsettings.Local.json` tùy chọn, bị bỏ qua bởi Git, có thể cung cấp các giá trị ghi đè cục bộ. Hãy cấu hình các giá trị thời gian chạy của máy chủ bằng user secrets hoặc biến môi trường, bao gồm chuỗi kết nối PostgreSQL, cấu hình JWT, GoogleCloud, CloudflareR2 và Mapbox. Không đặt bí mật thực trong các tệp cấu hình được theo dõi.

## Mobile API configuration and signing / Cấu hình API di động và ký ứng dụng

**English** — `src/NarrationApp.Mobile/Resources/Raw/visitor-api.json` contains sample/example endpoints. Replace it with a real configuration for a physical device or release build. Every `Staging` and `Release` build requires a real `VisitorApiConfigFile`, because the project fails the build when it is absent. Signing is configured separately with `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, and `ANDROID_KEY_PASSWORD`; these values are needed only for signed packages and keystore configuration (for example, in a local, untracked signing props file). Unsigned local validation remains possible.

**Tiếng Việt** — `src/NarrationApp.Mobile/Resources/Raw/visitor-api.json` chứa các điểm cuối mẫu/ví dụ. Hãy thay bằng cấu hình thực cho thiết bị vật lý hoặc bản dựng phát hành. Mọi bản dựng `Staging` và `Release` đều cần `VisitorApiConfigFile` thực, vì dự án sẽ làm cho quá trình xây dựng thất bại khi thiếu tệp này. Việc ký được cấu hình riêng bằng `ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` và `ANDROID_KEY_PASSWORD`; các giá trị này chỉ cần cho gói đã ký và cấu hình keystore (ví dụ trong tệp props ký cục bộ, không được theo dõi). Vẫn có thể kiểm tra cục bộ không ký.

## Provider behavior / Hành vi nhà cung cấp

**English**

- Without Mapbox, the web analytics map and mobile map/directions are unavailable.
- Without Google credentials, the server uses mock translation and text-to-speech services.
- Without Cloudflare R2 credentials, the server uses local mock storage.

**Tiếng Việt**

- Khi không có Mapbox, bản đồ phân tích trên web và bản đồ/chỉ đường trên di động không khả dụng.
- Khi không có thông tin xác thực Google, máy chủ dùng dịch vụ mô phỏng cho dịch thuật và chuyển văn bản thành giọng nói.
- Khi không có thông tin xác thực Cloudflare R2, máy chủ dùng bộ nhớ mô phỏng cục bộ.

## Security / Bảo mật

**English** — The repository includes development credential placeholders, a checked-in development JWT signing-key placeholder, and development signing-key placeholders. Do not reuse them outside local development. Store real settings in user secrets or environment variables, keep signing material out of source control, and rotate any value if it has been reused.

**Tiếng Việt** — Kho mã nguồn có các chỗ giữ chỗ cho thông tin xác thực phát triển, một chỗ giữ chỗ khóa ký JWT phát triển được lưu trong kho mã nguồn và các chỗ giữ chỗ khóa ký phát triển. Không sử dụng lại chúng ngoài môi trường phát triển cục bộ. Lưu cấu hình thực trong user secrets hoặc biến môi trường, giữ tài liệu ký ngoài hệ thống quản lý mã nguồn và xoay vòng mọi giá trị nếu đã từng được sử dụng lại.

## Reference documentation / Tài liệu tham khảo

**English / Tiếng Việt**

- [Reverse-proxy setup / Thiết lập reverse proxy](docs/reverse-proxy-setup.md)
- [Server production deployment / Triển khai máy chủ production](docs/server-production-deploy.md)
- [Public QR domain / Tên miền QR công khai](docs/public-qr-domain.md)
- [Citywide frontend design / Thiết kế giao diện toàn thành phố](docs/superpowers/specs/2026-07-17-sai-gon-ke-citywide-frontend-design.md)

## Roadmap and contribution / Lộ trình và đóng góp

**English** — Current work is moving the visitor-facing frontend toward broader Ho Chi Minh City coverage while retaining the District 4/Vĩnh Khánh context where seeded data, portal views, and analytics still depend on it. Contributions should preserve that distinction, avoid treating sample data as citywide evidence, and keep documentation in English and Vietnamese with matching meaning.

**Tiếng Việt** — Công việc hiện tại đang đưa giao diện hướng tới khách truy cập đến độ phủ rộng hơn trên khắp Thành phố Hồ Chí Minh, đồng thời giữ bối cảnh Quận 4/Vĩnh Khánh ở những nơi dữ liệu mẫu, giao diện cổng thông tin và phân tích vẫn phụ thuộc vào bối cảnh đó. Đóng góp cần giữ nguyên sự phân biệt này, không xem dữ liệu mẫu là bằng chứng về phạm vi toàn thành phố và duy trì tài liệu bằng tiếng Anh và tiếng Việt với ý nghĩa tương ứng.
