# Implementation Spec Reset — Web First theo PRD

> Workspace: `D:\VinhKhanhFoodStreet`  
> Source of truth: `docs/PRD_ThuyetMinhTuDong_v1.html` + current implementation trong `src/` và `tests/`  
> Mục tiêu của bản spec này: viết lại kế hoạch từ đầu theo đúng PRD, nhưng **ưu tiên hoàn thiện web portal Admin/Owner trước**, rồi mới chuyển sang mobile app Tourist.

---

## 1. Kết luận đã chốt

| Hạng mục | Quyết định |
|---|---|
| FE web | Giữ `Blazor WebAssembly` |
| Web persona | Chỉ có `Admin` và `POI Owner` |
| Tourist | Chỉ dành cho mobile app |
| Guest mode | Chỉ dành cho mobile app |
| Route `/` trên web | Chỉ dùng để redirect, không là landing page |
| Web public marketing site | Không làm |
| Register trên web | Chỉ cho `POI Owner` tự đăng ký |
| Owner sau đăng ký | `Pending approval`, chờ admin duyệt |
| QR | Dùng `external scan + deep link`, không làm in-app QR scanner |
| QR fallback public web page | Tạm thời không làm trong batch web-first này |
| Style web | Bám visual grammar của PRD: dark operations dashboard, sidebar cố định, topbar gọn, panel/table/stat tile rõ ràng |
| Translation stack | Dùng `Google Cloud Translation` |
| TTS stack | Dùng `Google Cloud Text-to-Speech` |
| Audio source model | Nội dung và audio tiếng Việt là nguồn chuẩn; audio đa ngôn ngữ được generate từ nguồn tiếng Việt và lưu `Cloudflare R2` |

---

## 2. Những gì PRD thực sự yêu cầu

### 2.1 Product split

PRD mô tả rõ hệ thống có 2 mặt:

- **Mobile app (.NET MAUI Android)** cho `Tourist`
- **Web portal (Blazor WASM)** cho `Admin` và `POI Owner`

### 2.2 Vai trò

#### Tourist

- app Android
- guest mode
- map discovery
- GPS/geofence auto narration
- xem POI
- tour playback
- QR deep link bằng scan ngoài app

#### POI Owner

- đăng nhập web portal
- CRUD POI thuộc phạm vi của mình
- đính kèm nội dung nguồn tiếng Việt trong POI (`file audio` hoặc `script TTS`)
- theo dõi trạng thái audio / bản dịch đã được admin xử lý
- gửi duyệt thay đổi

#### Admin

- đăng nhập web portal
- moderation
- analytics
- tour management
- user management
- giám sát thiết bị / trạng thái vận hành
- quản trị QR/deep link

### 2.3 Phần web trong PRD

Nếu chỉ tách phần web ra khỏi PRD, các module cần có là:

- login
- owner application / owner approval
- admin dashboard
- owner dashboard
- owner POI management (kèm nội dung nguồn tiếng Việt)
- admin audio management
- admin translation review
- moderation queue
- analytics dashboard
- tour management
- user management + device presence
- notification / realtime update
- geofence configuration
- QR/deep link management

---

## 3. Audit project hiện tại

## 3.1 Nền tảng đang có thể giữ lại

### Backend

Backend hiện đã có nền rất tốt và không cần phá đi làm lại:

- auth
- owner registration + pending approval
- POI
- translation
- audio
- moderation
- analytics
- tours + tour sessions
- notifications + SignalR
- QR service cơ bản
- geofence service cơ bản
- rate limiting
- request diagnostics

### Web portal

Web đã có phần lớn route chính:

- `/auth/login`
- `/auth/register`
- `/admin/dashboard`
- `/admin/moderation-queue`
- `/admin/analytics`
- `/admin/audio-management`
- `/admin/translation-review`
- `/admin/tour-management`
- `/admin/user-management`
- `/owner/dashboard`
- `/owner/poi-management`
- `/owner/audio-management` (cần gộp vào `Owner POI Management` theo PRD)
- `/owner/translation-management` (cần gộp vào `Owner POI Management` theo PRD)

### Verification

Project hiện đã có:

- web tests
- server tests
- browser smoke script

Điều này rất quan trọng vì cho phép refactor tiếp theo spec mà vẫn giữ được lưới an toàn.

## 3.2 Những gì đang lệch hoặc còn thiếu so với PRD

### Nhóm A — Lệch về scope và cleanup

Các mục này không đúng tinh thần web-first portal:

- còn file template thừa:
  - `src/NarrationApp.Web/Pages/Counter.razor`
  - `src/NarrationApp.Web/Pages/Weather.razor`
  - `src/NarrationApp.Web/Layout/NavMenu.razor`
  - `src/NarrationApp.Web/wwwroot/sample-data/weather.json`
- `Home.razor` hiện chỉ redirect, điều đó đúng hướng, nhưng vẫn nên coi đây là `root redirect shim`, không phải page sản phẩm
- `App.razor` vẫn còn copy kiểu `Phase 5`

### Nhóm B — Lệch về visual/copy so với PRD

Web đã bám PRD hơn trước, nhưng vẫn còn dấu vết của hướng cinematic/landing cũ:

- copy kiểu `studio`, `desk`, `command center`
- một số header/panel wording vẫn thiên showcase hơn là vận hành
- còn CSS/SharedUI remnants kiểu cinematic
- còn component cũ không còn là source of truth:
  - `OverviewHero`
  - `GeofenceRadarCard`
  - `TtsLiveFeedCard`
  - `TourProgressCard`
- còn CSS class cũ như `cinematic-grid`

### Nhóm C — Thiếu workflow web quan trọng

Đây là phần thiếu thật theo PRD:

1. **Owner geofence management**
- backend có `GeofencesController`
- owner dashboard hiện chỉ hiển thị summary số geofence
- owner POI management hiện chưa có form chỉnh geofence

2. **Admin QR / deep link management**
- backend hiện chỉ có `POST /api/qr`, `GET /api/qr/{code}`, `POST /api/qr/{code}/scan`
- web hiện chưa có workflow quản trị QR nào

3. **QR admin workflow còn thiếu API quản trị**
- chưa có list/search/filter QR codes
- chưa có deactivate/delete/regenerate
- chưa có route quản trị web tương ứng

4. **PRD copy normalization**
- route và page titles cần đổi sang ngôn ngữ vận hành rõ ràng, ngắn, bám PRD

5. **Audio / translation workflow của owner còn lệch PRD**
- owner hiện còn route/page riêng cho audio và translation
- trong PRD, owner chỉ quản lý `nội dung nguồn tiếng Việt` bên trong POI
- admin mới là nơi xử lý `Google Cloud Translation`, `Google Cloud Text-to-Speech` và generate audio đa ngôn ngữ

## 3.3 Những gì chỉ nên xem là khác biệt có chủ đích

Không phải mọi thứ khác PRD đều là lỗi. Có 2 khác biệt cần ghi rõ để tránh nhầm:

### A. Web public fallback cho QR

PRD có nhắc tình huống app chưa cài thì mở web fallback. Tuy nhiên quyết định hiện tại của dự án là:

- web chỉ có admin portal + owner portal
- không mở public site

Vì vậy:

- `QR fallback public page` được xem là **deferred**
- không thuộc batch web-first hiện tại

### B. Geofence model v1

Schema hiện hỗ trợ bảng `geofences`, nhưng service/update hiện tại đang vận hành theo hướng:

- **1 geofence chính cho mỗi POI**

Do đó, spec web-first sẽ chốt:

- v1 web chỉ hỗ trợ **primary geofence per POI**
- nếu cần multi-geofence phức tạp, mở ở phase sau

Điều này bám sát implementation hiện tại và tránh mở refactor backend không cần thiết trong batch web-first.

---

## 4. Spec web-first mới

## 4.1 Mục tiêu của batch web-first

Trước khi làm mobile, web phải đạt trạng thái:

- đúng persona
- đúng visual language PRD
- đúng workflow Admin/Owner
- không còn template thừa
- không còn nhầm lẫn web/tourist/public landing

## 4.2 Route map cuối cùng của web

### Public/Auth

- `/` → redirect theo session
- `/auth/login`
- `/auth/register`

### Admin

- `/admin/dashboard`
- `/admin/moderation-queue`
- `/admin/analytics`
- `/admin/audio-management`
- `/admin/translation-review`
- `/admin/tour-management`
- `/admin/user-management`

### Owner

- `/owner/dashboard`
- `/owner/poi-management`

Không thêm public routes khác trong batch này.

## 4.3 Sidebar/navigation model

Sidebar tiếp tục theo grammar đã chốt từ mock:

- brand block
- chia nhóm rõ
- icon + plain text
- không underline
- active row có highlight

### Admin navigation

#### Tổng quan

- Dashboard

#### Nội dung

- Audio Narration
- Bản dịch

#### Điều hành

- Tour
- Moderation

#### Hệ thống

- Analytics
- Người dùng

`QR & deep link` không cần thêm node sidebar riêng ở v1. Nó sẽ nằm như một workspace trong `Tour Management` để không làm sidebar phình ra.

### Owner navigation

#### Tổng quan

- Dashboard

#### Nội dung

- Quản lý POI

Geofence, nội dung nguồn tiếng Việt, trạng thái audio và trạng thái bản dịch không có nav riêng; tất cả nằm trong `Quản lý POI`.

## 4.4 Visual grammar bắt buộc

Toàn bộ web phải bám PRD theo các nguyên tắc này:

- nền `dark ops dashboard`
- sidebar cố định
- topbar/header gọn
- cards/panels có viền rõ
- stat tiles đồng nhất
- bảng dữ liệu dày vừa phải
- label mono cho meta/status
- tránh visual kể chuyện kiểu landing
- copy ngắn, thực dụng, tiếng Việt rõ nghĩa

### Những thứ phải loại bỏ dần

- `studio`
- `desk`
- `command center`
- `cinematic`
- hero copy quá kịch tính

---

## 5. Functional spec cho từng vùng web

## 5.1 Auth surface

### Login

`/auth/login` phải:

- chỉ dành cho admin và owner đã được duyệt
- chặn tourist vào web
- chặn owner đang pending
- chặn owner bị reject
- redirect đúng role sau login

### Register owner

`/auth/register` phải là **Owner Application Form** với đúng field:

- họ và tên
- email
- mật khẩu
- nhập lại mật khẩu

Không có:

- preferred language
- tourist signup semantics

Submit xong:

- hiện success state
- báo `Đã gửi yêu cầu đăng ký. Vui lòng chờ admin duyệt.`
- quay về login

## 5.2 Owner workspace

### Owner Dashboard

Mục tiêu:

- nhìn nhanh tình trạng POI, audio, translation, geofence, moderation

Acceptance:

- hiển thị counts rõ ràng
- có table/list POI
- thể hiện trạng thái publication
- thể hiện geofence summary
- thể hiện audio readiness
- có link sang POI management

### Owner POI Management

Đây là page owner quan trọng nhất.

Acceptance v1:

- chọn POI
- tạo POI
- cập nhật POI
- xóa POI
- đính kèm hoặc thay thế nội dung nguồn tiếng Việt (`file audio` hoặc `script TTS`)
- xem trạng thái audio đa ngôn ngữ và bản dịch đã được admin xử lý
- gửi duyệt
- xem moderation history
- không có workflow generate audio đa ngôn ngữ tại owner

### Owner geofence block bên trong POI Management

Spec chốt:

- không tạo page geofence riêng
- thêm `Primary Geofence` block trong `Owner POI Management`

Form phải có:

- name
- radius meters
- priority
- debounce seconds
- cooldown seconds
- trigger action
- nearest only
- active / inactive

Behavior:

- load geofence hiện tại của POI
- nếu chưa có thì tạo mới
- save bằng API geofence hiện có

### Nội dung nguồn tiếng Việt bên trong Owner POI Management

Acceptance:

- chọn POI
- upload hoặc thay file audio nguồn tiếng Việt
- nhập hoặc cập nhật script TTS tiếng Việt
- chọn rõ nguồn chuẩn đang dùng để admin generate audio đa ngôn ngữ
- xem trạng thái bản dịch và audio đã phát hành theo ngôn ngữ
- không có page audio riêng
- không có page translation riêng

## 5.3 Admin workspace

### Admin Dashboard

Acceptance:

- moderation backlog
- top POI / operational signals
- notification summary
- device / user presence summary

### Moderation Queue

Acceptance:

- hiển thị request pending
- phân biệt `poi` và `owner_registration`
- approve / reject
- lưu review note
- queue refresh sau action
- owner nhận notification

### Analytics

Acceptance:

- top POI
- audio plays
- heatmap / activity summary
- unread notification summary
- operational diagnostics

### User Management

Acceptance:

- list users
- role badge
- online/offline status
- last seen
- số thiết bị
- 3 card thiết bị:
  - tổng thiết bị
  - thiết bị online
  - thiết bị offline

Lưu ý:

- tourist vẫn có thể xuất hiện trong admin list vì vẫn là role của hệ thống
- nhưng tourist không phải web persona

### Tour Management

Acceptance:

- create/update/delete tour
- edit stops
- publish state
- published tours dùng cho mobile app

### QR & deep link workspace trong Tour Management

Đây là phần mới còn thiếu.

Spec chốt:

- không thêm sidebar riêng
- thêm `QR & Deep Link` block/workspace trong `Admin Tour Management`

Admin phải có thể:

- tạo QR cho `open_app`
- tạo QR cho `poi`
- tạo QR cho `tour`
- xem danh sách QR đã tạo
- lọc theo target type
- xem expiry
- deactivate hoặc xóa QR

Nếu API hiện tại chưa đủ, backend phải bổ sung.

### Admin Audio Management

Acceptance:

- chọn POI
- xem audio nguồn tiếng Việt và audio assets theo ngôn ngữ
- tạo audio nguồn tiếng Việt từ script TTS
- upload hoặc thay thế audio nguồn tiếng Việt
- generate audio đa ngôn ngữ từ nguồn tiếng Việt qua `Google Cloud Translation` + `Google Cloud Text-to-Speech`
- nghe thử audio theo ngôn ngữ
- chỉnh metadata
- xóa asset lỗi
- retry generate khi lỗi

### Admin Translation Review

Acceptance:

- chọn POI
- xem translation inventory theo nguồn tiếng Việt
- auto-translate bằng `Google Cloud Translation`
- chỉnh sửa
- lưu bản dịch
- dùng bản dịch đã review làm input cho bước generate audio đa ngôn ngữ

---

## 6. Backend alignment cho batch web-first

## 6.1 Giữ nguyên

Các phần backend hiện có thể giữ:

- owner registration flow
- login blocking logic
- moderation flow
- notification / SignalR
- tours
- analytics
- audio
- translation
- user/device summary

Lưu ý alignment theo PRD:

- `audio` và `translation` vẫn giữ làm module riêng
- nhưng provider phải chốt về `Google Cloud Translation` + `Google Cloud Text-to-Speech`
- audio tiếng Việt là nguồn chuẩn cho workflow generate audio đa ngôn ngữ
- file audio sinh ra được lưu trên `Cloudflare R2`

## 6.2 Cần làm thêm để web đủ theo spec

### Geofence

Với hướng `1 primary geofence / POI`, backend hiện gần đủ.

Cần đảm bảo:

- owner và admin đều update được geofence đúng POI
- validation đầy đủ cho radius/debounce/cooldown

### QR admin management

Backend hiện thiếu các API quản trị web-friendly. Cần bổ sung:

- `GET /api/qr`
  - list QR codes
  - filter theo target type
  - filter theo target id
- `DELETE /api/qr/{id}` hoặc `PUT /api/qr/{id}/deactivate`
- nếu cần, `POST /api/qr/{id}/regenerate`

### Audio + Translation theo Google stack

Backend cần khóa lại đúng flow PRD:

- nội dung tiếng Việt là nguồn chuẩn cho translation
- audio tiếng Việt là nguồn chuẩn cho audio pipeline
- text được dịch bằng `Google Cloud Translation`
- audio được synthesize bằng `Google Cloud Text-to-Speech`
- assets được lưu trên `Cloudflare R2`

API/admin workflow cần hỗ trợ rõ:

- `GET /api/audio?poiId={id}`
  - trả audio nguồn tiếng Việt + audio assets đa ngôn ngữ
- `POST /api/audio/upload`
  - upload audio nguồn tiếng Việt
- `POST /api/audio/tts`
  - tạo audio nguồn tiếng Việt từ script TTS tiếng Việt
- `POST /api/audio/{id}/generate-languages`
  - lấy nội dung tiếng Việt
  - dịch qua `Google Cloud Translation`
  - synthesize qua `Google Cloud Text-to-Speech`
  - upsert translation + audio asset theo ngôn ngữ
- `POST /api/translations/auto-translate`
  - dùng `Google Cloud Translation`
  - lưu bản dịch để admin review

Owner flow không cần API/page riêng cho audio management hoặc translation management; owner chỉ cập nhật nội dung nguồn tiếng Việt trong POI rồi gửi duyệt.

### Copy / error contract

Không bắt buộc đổi lớn trong batch này, nhưng response message cho web nên:

- ngắn
- rõ
- bám terminology của PRD

---

## 7. Web-first roadmap

## Phase W0 — Audit cleanup

Mục tiêu:

- dọn những gì không còn thuộc sản phẩm

Checklist:

- xóa `Counter.razor`
- xóa `Weather.razor`
- xóa `NavMenu.razor`
- xóa `wwwroot/sample-data/weather.json`
- dọn copy `Phase 5`
- đổi tên/copy các vùng còn đậm chất cinematic
- xóa component/CSS cũ không còn dùng

Done when:

- không còn route/template thừa
- solution build/test vẫn xanh

## Phase W1 — Auth và role boundary

Mục tiêu:

- khóa web thành đúng `Admin + Owner`

Checklist:

- login flow giữ đúng behavior hiện tại
- register owner đúng 4 field
- root redirect rõ ràng
- not found / auth copy đúng sản phẩm
- tourist chỉ là app-only role

Done when:

- toàn bộ auth surface bám spec
- không còn persona web dành cho tourist

## Phase W2 — Owner workspace complete

Mục tiêu:

- owner portal đủ dùng thật cho PRD

Checklist:

- owner dashboard polish theo PRD
- owner POI CRUD hoàn chỉnh
- moderation history rõ
- nội dung nguồn tiếng Việt trong POI management rõ ràng
- không còn owner audio page riêng
- không còn owner translation page riêng
- thêm `Primary Geofence` block vào POI management

Done when:

- owner có thể hoàn thành trọn flow:
  - tạo/sửa POI
  - chỉnh geofence
  - đính kèm audio nguồn hoặc script TTS tiếng Việt
  - theo dõi trạng thái audio / translation đã được admin xử lý
  - gửi duyệt

## Phase W3 — Admin workspace complete

Mục tiêu:

- admin portal đủ các module PRD yêu cầu

Checklist:

- moderation queue hoàn chỉnh
- analytics polish
- user/device monitoring hoàn chỉnh
- tour management hoàn chỉnh
- bổ sung QR/deep link workspace trong admin area
- audio management hoàn chỉnh theo `Google Cloud Translation` + `Google Cloud Text-to-Speech`
- translation review hoàn chỉnh theo `Google Cloud Translation`
- generate audio đa ngôn ngữ luôn xuất phát từ nguồn tiếng Việt

Done when:

- admin có thể:
  - duyệt owner application
  - duyệt nội dung
  - tạo audio nguồn tiếng Việt
  - generate audio đa ngôn ngữ từ nguồn tiếng Việt
  - review bản dịch bằng Google Cloud Translation
  - quản lý tour
  - quản lý QR/deep link
  - xem analytics
  - quản lý user/device

## Phase W4 — PRD visual polish + hardening

Mục tiêu:

- đồng bộ hết web theo visual/copy của PRD

Checklist:

- normalize title/header/subtitle toàn bộ screen
- bỏ hết wording `studio/desk/command center`
- thống nhất panel/table/badge/stat shell
- responsive ổn định desktop + mobile width portal
- browser smoke đủ route thật

Done when:

- web có cảm giác là một portal vận hành thống nhất, không còn pha landing/showcase

## Phase W5 — Release readiness cho web

Checklist:

- build xanh
- web tests xanh
- server tests xanh
- browser smoke xanh
- migration/database local chạy được
- issue list còn lại của web đã xuống mức chấp nhận được

Kết thúc Phase W5 nghĩa là:

- web portal được xem là xong phase chính
- có thể chuyển trọng tâm sang mobile

---

## 8. Mobile spec sau khi web xong

Mobile chỉ bắt đầu mạnh tay sau khi hoàn tất web-first batch.

## Phase M1 — App foundation

- auth optional
- guest mode
- settings ngôn ngữ
- POI discovery base

## Phase M2 — Map + geofence + narration

- map discovery
- nearby POI
- geofence engine
- cooldown / debounce
- audio playback + text fallback

## Phase M3 — QR deep link + tour

- external scan → deep link
- open app / poi / tour targets
- tour list + detail
- start/resume/progress cho signed-in tourist
- guest mode read-only

## Phase M4 — Mobile polish

- cache
- offline behavior tối thiểu
- battery / permission UX
- narration completion analytics

---

## 9. Explicit out of scope cho batch hiện tại

Các mục sau không thuộc web-first batch:

- public landing page
- tourist web dashboard
- in-app QR scanner
- QR fallback public website
- payment
- chatbot AI
- offline map toàn phần
- iOS app

---

## 10. Acceptance checklist cuối cùng

Web chỉ được xem là “xong trước mobile” khi thỏa hết các điều sau:

- không còn page template thừa
- không còn tourist web surface
- login/register owner đúng spec
- sidebar/topbar/panel/table bám PRD
- owner flow đủ:
  - POI
  - geofence
  - nội dung nguồn tiếng Việt
  - moderation submit
- admin flow đủ:
  - moderation
  - owner approval
  - analytics
  - user/device
  - tours
  - QR/deep link
  - audio theo Google stack
  - translation theo Google stack
- notification center + SignalR vẫn hoạt động
- test/build/smoke xanh

---

## 11. Gợi ý triển khai tiếp theo

Nếu bám đúng spec mới này, thứ tự thực thi nên là:

1. `Phase W0` cleanup template + copy cleanup
2. `Phase W2` owner geofence block trong POI management
3. `Phase W3` admin QR/deep link workspace + API bổ sung
4. `Phase W4` normalize visual/copy toàn bộ web
5. `Phase W5` verify web hoàn chỉnh
6. mới chuyển sang mobile

Đây là thứ tự hợp lý nhất vì:

- ít phá nền hiện có
- khóa web đúng PRD trước
- không mở rộng mobile khi web còn gap rõ ràng
