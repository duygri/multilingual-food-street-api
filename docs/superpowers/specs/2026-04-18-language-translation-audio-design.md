# Language, Translation, and Audio Pipeline Design

**Date:** 2026-04-18  
**Scope:** Admin web portal only  
**Status:** Approved for implementation in this session

---

## Goal

Chuẩn hóa lại pipeline nội dung đa ngôn ngữ của hệ thống theo một nguồn sự thật duy nhất:

- `Tiếng Việt (text/script)` là nguồn chuẩn
- `Google Cloud Translation` tạo bản dịch từ nguồn tiếng Việt
- `Google Cloud Text-to-Speech` tạo audio từ bản dịch đã lưu
- `Audio tiếng Việt` là asset của ngôn ngữ `vi`, không phải nguồn để dịch

Điều này thay thế cách hiểu cũ dễ gây lệch dữ liệu khi màn `Audio` tự dịch rồi generate trực tiếp trong cùng một thao tác.

---

## Product Decisions

### 1. Source of truth

- Mỗi POI có nội dung nguồn tiếng Việt trong `Poi` và `PoiTranslation(language=vi)`.
- Nội dung nguồn tiếng Việt là bản gốc để tạo toàn bộ translation và audio đa ngôn ngữ.
- Mọi bản dịch không phải `vi` đều được xem là dẫn xuất của bản gốc tiếng Việt.

### 2. Translation first, audio second

- `Translation Review` là nơi tạo, rà soát và lưu translation.
- `Audio Management` chỉ generate audio khi translation của ngôn ngữ đó đã tồn tại.
- Màn `Audio` không còn tự gọi translate ngầm trước khi generate.

### 3. Language catalog

- Admin có workspace `Quản lý ngôn ngữ`.
- Workspace này quản lý danh sách ngôn ngữ đang bật cho hệ thống web/mobile.
- `vi` luôn là ngôn ngữ nguồn chuẩn và không được xóa.
- Các ngôn ngữ khác có thể bật/tắt hoặc thêm mới từ danh sách Google hỗ trợ.

### 4. Coverage model

- `Quản lý ngôn ngữ` hiển thị coverage theo ngôn ngữ:
  - số POI đã có translation
  - số audio assets
  - vai trò ngôn ngữ
  - trạng thái active
- `Bản dịch` hiển thị coverage theo POI x ngôn ngữ.
- `Audio` hiển thị asset theo POI x ngôn ngữ, dựa trên translation đã lưu.

---

## Data Model Adjustments

### Language catalog

Thêm bảng quản trị ngôn ngữ, ví dụ `managed_languages`, với các trường:

- `Code`
- `DisplayName`
- `NativeName`
- `FlagCode`
- `Role` (`source`, `translation_audio`)
- `IsActive`
- `CreatedAtUtc`

### Translation state

Mở rộng `PoiTranslation` để biểu diễn trạng thái vận hành:

- `Status`
  - `Source`
  - `AutoTranslated`
  - `Reviewed`
- `UpdatedAtUtc`

Quy ước:

- `vi` luôn có `Status = Source`
- translation do Google tạo ra ban đầu là `AutoTranslated`
- admin sửa tay hoặc xác nhận lại thì chuyển sang `Reviewed`

### Audio lineage

Mở rộng `AudioAsset` để biết audio được sinh từ translation nào:

- `TranslationId` nullable
- `VoiceProfile`
- `UpdatedAtUtc`

Quy ước:

- audio `vi` có thể gắn vào translation `vi` nếu tồn tại
- audio ngoài `vi` phải gắn với translation tương ứng
- server không generate audio đa ngôn ngữ nếu translation chưa tồn tại

---

## Admin Workspaces

### 1. Quản lý ngôn ngữ

Route đề xuất:

- `/admin/language-management`

Sidebar:

- Group `Hệ thống`
- Label `Ngôn ngữ`

Màn hình này hiển thị:

- danh sách ngôn ngữ đang bật
- coverage translation/audio theo từng ngôn ngữ
- vai trò `Nguồn chuẩn` hoặc `TTS + Dịch`
- nút thêm ngôn ngữ mới

### 2. Bản dịch

Route giữ nguyên:

- `/admin/translation-review`

Màn hình này đổi mục tiêu sang:

- hiển thị ma trận `POI x ngôn ngữ`
- auto-translate cho một POI hoặc hàng loạt
- hiển thị tiến độ dịch của từng POI
- phân biệt `Source / Auto / Reviewed`

### 3. Audio

Route giữ nguyên:

- `/admin/audio-management`

Màn hình này đổi mục tiêu sang:

- quản lý audio nguồn tiếng Việt
- generate audio từ translation đã lưu
- không còn tự translate trong thao tác generate
- hiển thị loại `Recorded` hoặc `TTS`
- hiển thị trạng thái `Ready / Generating / Failed`

---

## API Changes

### Languages

- `GET /api/admin/languages`
- `POST /api/admin/languages`
- `DELETE /api/admin/languages/{code}`

### Translations

Giữ:

- `GET /api/translations?poiId={id}`
- `POST /api/translations`
- `POST /api/translations/{poiId}/auto?targetLanguage=...`
- `DELETE /api/translations/{id}`

Mở rộng hành vi:

- `auto` tạo translation với `Status = AutoTranslated`
- `save` trên `vi` giữ `Status = Source`
- `save` trên ngôn ngữ khác chuyển thành `Reviewed`

### Audio

Giữ:

- `GET /api/audio?poiId={id}`
- `POST /api/audio/upload`
- `POST /api/audio/tts`

Thêm:

- `POST /api/audio/generate-from-translation`

Request:

- `PoiId`
- `LanguageCode`
- `VoiceProfile`

Server flow:

1. tìm translation đã lưu của `PoiId + LanguageCode`
2. chọn text ưu tiên `Story -> Description -> Highlight -> Title`
3. gọi Google TTS
4. upsert `AudioAsset`
5. gắn `TranslationId`

---

## UI Behavior Rules

### Language management

- `vi` luôn hiển thị `Nguồn chuẩn`
- ngôn ngữ không active không xuất hiện trong workflow generate mặc định
- thêm ngôn ngữ mới sẽ làm nó xuất hiện ở `Bản dịch` và `Audio`

### Translation review

- `Auto-translate All` chỉ chạy trên các ngôn ngữ active, trừ `vi`
- nếu POI chưa có source `vi`, không được auto-translate
- chỉnh tay translation sẽ đổi trạng thái sang `Reviewed`

### Audio management

- nếu POI chưa có audio `vi`, admin vẫn có thể:
  - upload audio nguồn tiếng Việt
  - tạo TTS tiếng Việt từ script
- nếu ngôn ngữ chưa có translation, nút generate audio phải disabled hoặc báo rõ lý do
- generate audio hàng loạt chỉ chạy trên các ngôn ngữ active đã có translation

---

## Non-Goals for This Iteration

- Chưa làm mobile
- Chưa thêm version hash để invalidation tự động
- Chưa làm background queue thực sự cho batch translation/audio
- Chưa làm custom domain workflow cho R2

---

## Implementation Order

1. Thêm language catalog và seed ngôn ngữ mặc định
2. Thêm metadata trạng thái cho translation/audio
3. Đổi backend audio sang generate từ translation đã lưu
4. Thêm `Quản lý ngôn ngữ` vào sidebar + route + page
5. Sửa màn `Bản dịch` sang matrix view
6. Sửa màn `Audio` sang translation-driven workflow
7. Verify lại server tests và web tests
