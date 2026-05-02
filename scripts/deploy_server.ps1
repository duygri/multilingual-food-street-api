param(
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Production",

    [string]$Framework = "net8.0",

    [string]$OutputRoot = "",

    [string]$ConnectionString = "",

    [string]$Urls = "http://0.0.0.0:5000",

    [switch]$NoRestore,

    [switch]$SkipZip,

    [switch]$RunAfterPublish
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "src\NarrationApp.Server\NarrationApp.Server.csproj"
$configuration = if ($Environment -eq "Development") { "Debug" } else { "Release" }

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "artifacts\server-publish"
}

$environmentKey = $Environment.ToLowerInvariant()
$outputPath = Join-Path $OutputRoot $environmentKey
$zipPath = Join-Path $OutputRoot ("narrationapp-server-" + $environmentKey + ".zip")

if (Test-Path $outputPath) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$arguments = @(
    "publish",
    $projectPath,
    "-c", $configuration,
    "-f", $Framework,
    "-o", $outputPath,
    "-p:ContinuousIntegrationBuild=true"
)

if ($NoRestore) {
    $arguments += "--no-restore"
}

Write-Host "Environment : $Environment"
Write-Host "Configuration: $configuration"
Write-Host "Framework    : $Framework"
Write-Host "OutputPath   : $outputPath"

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not $SkipZip) {
    if (Test-Path $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -Path (Join-Path $outputPath "*") -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "ZipPath      : $zipPath"
}

$serverExePath = Join-Path $outputPath "NarrationApp.Server.exe"

if ($RunAfterPublish) {
    if (-not (Test-Path $serverExePath)) {
        throw "Server executable '$serverExePath' was not found."
    }

    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $env:ASPNETCORE_URLS = $Urls

    if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
        $env:ConnectionStrings__PostgreSql = $ConnectionString
    }

    Write-Host "Starting published server..."
    Write-Host "  ASPNETCORE_ENVIRONMENT=$Environment"
    Write-Host "  ASPNETCORE_URLS=$Urls"
    if (-not [string]::IsNullOrWhiteSpace($ConnectionString)) {
        Write-Host "  ConnectionStrings__PostgreSql=<provided>"
    }

    & $serverExePath
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Next step on server:"
Write-Host "  1. Copy '$outputPath' (or '$zipPath') to the target host."
Write-Host "  2. Set ASPNETCORE_ENVIRONMENT=$Environment"
Write-Host "  3. Set ASPNETCORE_URLS=$Urls"
Write-Host "  4. Set ConnectionStrings__PostgreSql to the real PostgreSQL connection string."
Write-Host "  5. Start NarrationApp.Server.exe from the published folder."
Write-Host ""
Write-Host "Important production checks:"
Write-Host "  - https://narration.app/.well-known/assetlinks.json"
Write-Host "  - https://narration.app/qr/<sample-code>"
Write-Host "  - PublicQr__BaseUrl must match the public HTTPS domain"
