# Google Cloud Auth Setup

Backend `FoodStreet.Server` hiện hỗ trợ 3 chế độ auth cho Google Cloud:

- `Service account JSON`
- `ADC / gcloud`
- `API key`

Thứ tự ưu tiên runtime hiện tại:

1. `Service account JSON`
2. `API key`
3. `ADC / gcloud`

## 1. Service account JSON

Đây là cách phù hợp nhất khi backend cần chạy ổn định và đồng bộ `Translate + TTS`.

### Thiết lập nhanh trên Windows

```powershell
setx GOOGLE_APPLICATION_CREDENTIALS "C:\secrets\foodstreet-gcp.json"
```

Sau đó mở terminal / Visual Studio mới rồi chạy lại backend.

Bạn cũng có thể cấu hình trực tiếp trong:

- `FoodStreet.Server/appsettings.Development.json`

```json
"GoogleCloud": {
  "UseServiceAccountJson": true,
  "CredentialPath": "C:\\secrets\\foodstreet-gcp.json",
  "ProjectId": "YOUR_PROJECT_ID"
}
```

### Quyền service account khuyến nghị

- `Cloud Translation API User`
- `Service Usage Consumer`

Nếu dùng impersonation cho local dev, user của bạn cần thêm:

- `Service Account Token Creator`

## 2. API key mode

Set `GoogleCloud:ApiKey` trong `FoodStreet.Server/appsettings.Development.json`.

Mode này phù hợp khi:

- cần local dev nhanh
- chưa dùng được service account/ADC

`ProjectId` nên vẫn được cấu hình để health/status rõ ràng hơn.

## 3. ADC / gcloud mode

```powershell
gcloud auth login
gcloud config set project YOUR_PROJECT_ID
gcloud auth application-default login
gcloud auth application-default set-quota-project YOUR_PROJECT_ID
```

Nếu organization chặn tạo JSON key, đây là local-development flow nên dùng.

## 4. Optional: service account impersonation

```powershell
gcloud auth application-default login --impersonate-service-account=foodstreet-server@YOUR_PROJECT_ID.iam.gserviceaccount.com
gcloud auth application-default set-quota-project YOUR_PROJECT_ID
```

## 5. App configuration

Các field backend đang dùng:

```json
"GoogleCloud": {
  "ApiKey": "",
  "ProjectId": "",
  "CliPath": "gcloud",
  "UseServiceAccountJson": true,
  "CredentialPath": ""
}
```

## 6. Runtime diagnostics

Bạn có thể xem backend đang chạy mode nào tại:

- `GET /api/adminaudio/status`
- `POST /api/adminaudio/health-check`
- trang `TTS Console`

Các trạng thái sẽ phân biệt rõ:

- `Service account JSON`
- `API key`
- `ADC / gcloud`

`health-check` sẽ gọi thử:

- `Translate` với một câu mẫu sang `en-US`
- `TTS` với một câu mẫu `vi-VN`

Nếu `Translate` ra kết quả nhưng không đổi so với input, hoặc `TTS` không trả về `StaticUrl`, hãy kiểm tra lại quota, role và credential mode.

## 7. Components đang dùng Google Cloud auth

- `FoodStreet.Server/Services/Audio/GoogleTranslator.cs`
- `FoodStreet.Server/Services/Audio/GoogleTtsService.cs`
- `FoodStreet.Server/Services/GoogleCloud/GoogleCloudAccessTokenProvider.cs`
