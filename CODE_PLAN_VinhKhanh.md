# Code Plan — Thuyết Minh Đa Ngôn Ngữ Vĩnh Khánh (.NET)

Tài liệu chuẩn (source of truth): `C:\Users\letro\source\repos\PROJECT C#\PRD_UML_VinhKhanh_CSharp.html`

## Roadmap hiện tại

Master roadmap mới nhất:
- `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\plans\2026-04-01-master-roadmap-vinh-khanh.md`

Plan chi tiết mobile native map:
- `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\plans\2026-04-01-mobile-android-native-map-core-first.md`

Spec mobile native map:
- `C:\Users\letro\source\repos\PROJECT C#\docs\superpowers\specs\2026-04-01-mobile-android-native-map-design.md`

## Thứ tự chạy khuyến nghị

1. Chuẩn hóa `Google Cloud credential flow` cho `Translate + TTS`
2. Chốt lại `auth contract` còn dang dở
3. Ổn định `web map` làm baseline
4. Thực thi `mobile Android native map phase 1`
5. Làm tiếp `mobile native advanced` (`Places`, `geofence`)
6. Hoàn thiện `docs + smoke checklist + handoff`

## Trạng thái hiện tại

Đã xong:

- `Google Cloud credential flow` đã đọc được `service account JSON`, `API key`, và `ADC / gcloud`
- `auth contract` đã bỏ refresh token giả, login surface khớp behavior thật
- `web map` đã chạy trên `Google Maps JavaScript API`
- `mobile Android native map phase 1` đã có:
  - browse map native
  - focus POI
  - recenter
  - walking directions
  - native picker cho owner/admin
- `mobile native advanced` đã có:
  - Places autocomplete trong native picker
  - geofence circle overlay trên native map
  - `GeofenceTest` giữ vai trò admin/browser-only
- `TTS Console` đã có:
  - runtime status
  - quick TTS test
  - queue POI TTS
  - task monitor
  - `Google Cloud health check`
- backend local smoke hiện đã sạch hơn:
  - không còn crash vì `Windows EventLog`
  - không còn warning `DataProtection` khi chạy local/sandbox
  - profile `http` không còn warning `https port`
  - `GET /api/adminaudio/status` chạy được và nhận đúng `Service account JSON`
  - `POST /api/adminaudio/health-check` đã trả diagnostic chi tiết cho `auth / translate / tts`

## Việc còn lại nên làm tiếp

1. Manual smoke test thật trên web browser và emulator/device Android
2. Test Google Cloud ngoài sandbox có internet để xác nhận `Translate + TTS` trả kết quả thật
3. Nếu cần làm sạch sâu hơn, thay widget autocomplete deprecated bằng request-based predictions API
4. Polish thêm geofence UX hoặc analytics nếu scope demo yêu cầu

## Ghi chú

- Web tiếp tục dùng `Google Maps JavaScript API`.
- Mobile Android chuyển dần sang `Maps SDK for Android`.
- Không commit key Android thật hoặc service account JSON vào repo.
- Khi tiếp tục code, ưu tiên mở master roadmap trước rồi đi theo từng task/commit nhỏ.
- Trạng thái build gần nhất:
  - `FoodStreet.Server`: `0 warning / 0 error`
  - `SharedUI`: `0 warning / 0 error`
  - `Frontend`: `0 warning / 0 error`
  - `Mobile (net9.0-android)`: `0 warning / 0 error`
- Trạng thái smoke runtime gần nhất:
  - `GET /api/adminaudio/status`: `200 OK`
  - `POST /api/adminaudio/health-check`: `200 OK`
  - `POST /api/content/auth/login`: `200 OK`
  - `GET /api/content/auth/me`: `200 OK`
  - `GET /api/content/auth/debug/claims`: `200 OK`
  - `GET /api/qrcode`: `200 OK`
  - `GET /api/qrcode/{id}/meta`: `200 OK`
  - `GET /api/qrcode/{id}`: `200 OK`, trả `image/png`
  - `GET /api/maps/locations/{id}`: `200 OK`
  - `GET /api/maps/locations/near`: `200 OK`
  - Trong môi trường Codex hiện tại, Google Cloud bị chặn outbound network nên health check trả `authProbe = false` với thông điệp nguyên nhân chi tiết; đây không phải lỗi compile/runtime của app.
  - Dữ liệu hiện tại chưa có `tour` active, nên smoke runtime cho `start / resume / progress` cần tạo tour trước hoặc seed thêm dữ liệu.
