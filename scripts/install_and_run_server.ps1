param(
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Production",

    [string]$SourcePath = "",

    [string]$InstallRoot = "C:\apps\narrationapp-server",

    [string]$Urls = "http://0.0.0.0:5000",

    [string]$ConnectionString = "",

    [string]$PublicQrBaseUrl = "",

    [ValidateSet("None", "Foreground", "Background")]
    [string]$StartMode = "None"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$environmentKey = $Environment.ToLowerInvariant()

if ([string]::IsNullOrWhiteSpace($SourcePath)) {
    $zipCandidate = Join-Path $repoRoot ("artifacts\server-publish\narrationapp-server-" + $environmentKey + ".zip")
    $folderCandidate = Join-Path $repoRoot ("artifacts\server-publish\" + $environmentKey)

    if (Test-Path $zipCandidate) {
        $SourcePath = $zipCandidate
    }
    elseif (Test-Path $folderCandidate) {
        $SourcePath = $folderCandidate
    }
    else {
        throw "No default publish artifact found for environment '$Environment'. Run scripts/deploy_server.ps1 first or pass -SourcePath."
    }
}

$resolvedSourcePath = (Resolve-Path $SourcePath).Path
$installRootPath = [System.IO.Path]::GetFullPath($InstallRoot)
$incomingPath = Join-Path $installRootPath "incoming"
$currentPath = Join-Path $installRootPath "current"
$backupRoot = Join-Path $installRootPath "backup"
$logsPath = Join-Path $installRootPath "logs"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"

Write-Host "Environment : $Environment"
Write-Host "SourcePath  : $resolvedSourcePath"
Write-Host "InstallRoot : $installRootPath"
Write-Host "StartMode   : $StartMode"

New-Item -ItemType Directory -Path $installRootPath -Force | Out-Null
New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
New-Item -ItemType Directory -Path $logsPath -Force | Out-Null

if (Test-Path $incomingPath) {
    Remove-Item -LiteralPath $incomingPath -Recurse -Force
}

New-Item -ItemType Directory -Path $incomingPath -Force | Out-Null

if ([System.IO.Path]::GetExtension($resolvedSourcePath).Equals(".zip", [System.StringComparison]::OrdinalIgnoreCase)) {
    Expand-Archive -Path $resolvedSourcePath -DestinationPath $incomingPath -Force
}
else {
    Copy-Item -Path (Join-Path $resolvedSourcePath "*") -Destination $incomingPath -Recurse -Force
}

if (-not (Test-Path (Join-Path $incomingPath "NarrationApp.Server.exe"))) {
    throw "Incoming artifact does not contain NarrationApp.Server.exe."
}

if (Test-Path $currentPath) {
    $backupPath = Join-Path $backupRoot ("current-" + $timestamp)
    Move-Item -LiteralPath $currentPath -Destination $backupPath
    Write-Host "Backed up previous deployment to: $backupPath"
}

Move-Item -LiteralPath $incomingPath -Destination $currentPath

$runnerPath = Join-Path $installRootPath "run-server.ps1"
$runnerContent = @"
param(
    [ValidateSet("Foreground", "Background")]
    [string]`$Mode = "Foreground"
)

`$ErrorActionPreference = "Stop"
`$env:ASPNETCORE_ENVIRONMENT = "$Environment"
`$env:ASPNETCORE_URLS = "$Urls"
`$env:ConnectionStrings__PostgreSql = "$ConnectionString"
"@

if (-not [string]::IsNullOrWhiteSpace($PublicQrBaseUrl)) {
    $runnerContent += "`r`n`$env:PublicQr__BaseUrl = `"$PublicQrBaseUrl`""
}

$runnerContent += @"

if ([string]::IsNullOrWhiteSpace(`$env:ConnectionStrings__PostgreSql)) {
    throw "ConnectionStrings__PostgreSql is empty. Edit run-server.ps1 or pass -ConnectionString when installing."
}

`$installRoot = Split-Path -Parent `$PSCommandPath
`$currentPath = Join-Path `$installRoot "current"
`$logsPath = Join-Path `$installRoot "logs"
`$exePath = Join-Path `$currentPath "NarrationApp.Server.exe"

if (-not (Test-Path `$exePath)) {
    throw "NarrationApp.Server.exe was not found at `$exePath"
}

New-Item -ItemType Directory -Path `$logsPath -Force | Out-Null

if (`$Mode -eq "Background") {
    `$suffix = Get-Date -Format "yyyyMMdd-HHmmss"
    `$stdoutPath = Join-Path `$logsPath ("server-" + `$suffix + ".stdout.log")
    `$stderrPath = Join-Path `$logsPath ("server-" + `$suffix + ".stderr.log")

    `$process = Start-Process -FilePath `$exePath -WorkingDirectory `$currentPath -RedirectStandardOutput `$stdoutPath -RedirectStandardError `$stderrPath -WindowStyle Hidden -PassThru
    Write-Host "Started NarrationApp.Server in background. PID=`$(`$process.Id)"
    Write-Host "stdout: `$stdoutPath"
    Write-Host "stderr: `$stderrPath"
    exit 0
}

& `$exePath
exit `$LASTEXITCODE
"@

Set-Content -Path $runnerPath -Value $runnerContent -Encoding UTF8

Write-Host "Installed artifact to: $currentPath"
Write-Host "Runner script       : $runnerPath"

switch ($StartMode) {
    "Foreground" {
        & powershell -ExecutionPolicy Bypass -File $runnerPath -Mode Foreground
        exit $LASTEXITCODE
    }
    "Background" {
        & powershell -ExecutionPolicy Bypass -File $runnerPath -Mode Background
        exit $LASTEXITCODE
    }
    default {
        Write-Host "Next command:"
        Write-Host "  powershell -ExecutionPolicy Bypass -File `"$runnerPath`" -Mode Background"
    }
}
