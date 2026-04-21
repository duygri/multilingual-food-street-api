param(
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",

    [string]$ApiConfigFile = "",

    [string]$VersionName = "",

    [int]$VersionCode = 0,

    [ValidateSet("apk", "aab")]
    [string]$PackageFormat = "",

    [string]$Framework = "net9.0-android35.0",

    [string]$OutputRoot = "",

    [switch]$NoRestore
)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\NarrationApp.Mobile\NarrationApp.Mobile.csproj"

$configuration = switch ($Environment) {
    "Development" { "Debug" }
    "Staging" { "Staging" }
    "Production" { "Release" }
}

if ([string]::IsNullOrWhiteSpace($PackageFormat)) {
    $PackageFormat = if ($Environment -eq "Production") { "aab" } else { "apk" }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\mobile-publish"
}

$outputPath = Join-Path $OutputRoot $Environment.ToLowerInvariant()

$arguments = @(
    "publish",
    $projectPath,
    "-c", $configuration,
    "-f", $Framework,
    "-o", $outputPath,
    "-p:AndroidPackageFormats=$PackageFormat",
    "-p:ContinuousIntegrationBuild=true"
)

if (-not [string]::IsNullOrWhiteSpace($ApiConfigFile)) {
    if (-not (Test-Path $ApiConfigFile)) {
        throw "ApiConfigFile '$ApiConfigFile' was not found."
    }

    $resolvedApiConfigFile = (Resolve-Path $ApiConfigFile).Path
    $arguments += "-p:TouristApiConfigFile=$resolvedApiConfigFile"
}

if (-not [string]::IsNullOrWhiteSpace($VersionName)) {
    $arguments += "-p:MOBILE_VERSION_NAME=$VersionName"
}

if ($VersionCode -gt 0) {
    $arguments += "-p:MOBILE_VERSION_CODE=$VersionCode"
}

if ($NoRestore) {
    $arguments += "--no-restore"
}

Write-Host "Environment : $Environment"
Write-Host "Configuration: $configuration"
Write-Host "PackageFormat: $PackageFormat"
Write-Host "OutputPath   : $outputPath"

if ($Environment -in @("Staging", "Production") -and [string]::IsNullOrWhiteSpace($env:ANDROID_KEYSTORE_PATH)) {
    Write-Warning "ANDROID_KEYSTORE_PATH is not set. Build can still run, but signed package output depends on your local signing setup."
}

& dotnet @arguments
exit $LASTEXITCODE
