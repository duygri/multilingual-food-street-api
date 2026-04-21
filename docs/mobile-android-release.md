# Mobile Android Build And Release

## Muc tieu

Tai lieu nay chot cach build `Debug`, `Staging`, `Release` cho app mobile du lich, dong thoi tach ro:

- base URL API theo moi truong
- app identity theo moi config
- signing secret khong commit vao repo
- lenh publish Android cho tester hoac production

## App identity theo configuration

`NarrationApp.Mobile.csproj` hien tai dang map nhu sau:

- `Debug`
: `ApplicationId = com.foodstreet.tourist.dev`
: `ApplicationTitle = Food Street Tourist Dev`
: package mac dinh `apk`
- `Staging`
: `ApplicationId = com.foodstreet.tourist.staging`
: `ApplicationTitle = Food Street Tourist Staging`
: package mac dinh `apk`
- `Release`
: `ApplicationId = com.foodstreet.tourist`
: `ApplicationTitle = Food Street Tourist`
: package mac dinh `aab`

Dieu nay cho phep cai song song ban `dev`, `staging`, `production` tren Android de smoke va kiem thu.

## API config dong goi cung app

Mac dinh app doc file:

- [tourist-api.json](/D:/VinhKhanhFoodStreet/src/NarrationApp.Mobile/Resources/Raw/tourist-api.json)

Neu can dong goi mot file config rieng luc build, truyen:

- `/p:TouristApiConfigFile=C:\path\to\tourist-api.production.json`

File duoc truyen vao se duoc pack vao app voi ten logic `tourist-api.json`.

Shape JSON:

```json
{
  "development": {
    "default": "https://localhost:5001/",
    "android": "https://10.0.2.2:5001/"
  },
  "staging": {
    "default": "https://api.staging.narration.app/",
    "android": "https://api.staging.narration.app/"
  },
  "production": {
    "default": "https://api.narration.app/",
    "android": "https://api.narration.app/"
  }
}
```

## Signing secrets

Khong commit keystore hoac mat khau vao repo.

Co 2 cach truyen signing config:

1. Dung environment variables:

- `ANDROID_KEYSTORE_PATH`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`

2. Hoac copy:

- [mobile-signing.sample.props](/D:/VinhKhanhFoodStreet/src/NarrationApp.Mobile/mobile-signing.sample.props)

thanh:

- `src/NarrationApp.Mobile/mobile-signing.local.props`

File local nay da duoc ignore.

## Version metadata

Co the override version khi build:

- `MOBILE_VERSION_NAME`
- `MOBILE_VERSION_CODE`

Vi du:

```powershell
$env:MOBILE_VERSION_NAME = "1.2.0"
$env:MOBILE_VERSION_CODE = "12"
```

## Publish bang script

Script:

- [mobile_android_publish.ps1](/D:/VinhKhanhFoodStreet/scripts/mobile_android_publish.ps1)

Vi du:

```powershell
.\scripts\mobile_android_publish.ps1 -Environment Development
.\scripts\mobile_android_publish.ps1 -Environment Staging -ApiConfigFile C:\configs\foodstreet\tourist-api.staging.json
.\scripts\mobile_android_publish.ps1 -Environment Production -ApiConfigFile C:\configs\foodstreet\tourist-api.production.json -VersionName 1.0.0 -VersionCode 1
```

Mac dinh:

- `Development` => `Debug` + `apk`
- `Staging` => `Staging` + `apk`
- `Production` => `Release` + `aab`

## Publish bang dotnet truc tiep

```powershell
dotnet publish .\src\NarrationApp.Mobile\NarrationApp.Mobile.csproj `
  -c Release `
  -f net9.0-android35.0 `
  -p:AndroidPackageFormats=aab `
  -p:TouristApiConfigFile=C:\configs\foodstreet\tourist-api.production.json `
  -p:MOBILE_VERSION_NAME=1.0.0 `
  -p:MOBILE_VERSION_CODE=1
```

## Luu y

- `Debug/Staging` van build duoc neu chua co signing key.
- `Staging/Release` se tu dong nhat signing config neu `ANDROID_KEYSTORE_PATH` co gia tri.
- Neu truyen `TouristApiConfigFile` ma file khong ton tai, build se fail som voi loi ro rang.
