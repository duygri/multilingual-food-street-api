param(
    [string]$SiteName = "narration.app",

    [string]$HostName = "narration.app",

    [string]$PhysicalPath = "C:\inetpub\narrationapp-proxy",

    [string]$CertificateThumbprint = "",

    [int]$BackendPort = 5000
)

$ErrorActionPreference = "Stop"

if (-not (Get-Module -ListAvailable -Name WebAdministration))
{
    throw @"
The WebAdministration module is not available on this server.

Install IIS prerequisites first:
  powershell -ExecutionPolicy Bypass -File .\install_iis_prerequisites.ps1

Equivalent direct command on Windows Server:
  Install-WindowsFeature Web-Server, Web-Mgmt-Tools, Web-Scripting-Tools

Equivalent direct command on Windows 10/11:
  Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-ManagementScriptingTools, IIS-ManagementConsole -All

After that, install URL Rewrite and Application Request Routing (ARR), then rerun this script.
"@
}

Import-Module WebAdministration

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint))
{
    throw "CertificateThumbprint is required."
}

$resolvedPhysicalPath = [System.IO.Path]::GetFullPath($PhysicalPath)
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sampleConfigPath = Join-Path $repoRoot "deploy\reverse-proxy\iis\web.config"
$targetConfigPath = Join-Path $resolvedPhysicalPath "web.config"

if (-not (Test-Path $sampleConfigPath))
{
    throw "Sample IIS web.config was not found at $sampleConfigPath"
}

New-Item -ItemType Directory -Path $resolvedPhysicalPath -Force | Out-Null
Copy-Item -Path $sampleConfigPath -Destination $targetConfigPath -Force

[xml]$webConfig = Get-Content $targetConfigPath
$rewriteAction = $webConfig.SelectSingleNode("/configuration/system.webServer/rewrite/rules/rule/action")
if ($null -eq $rewriteAction)
{
    throw "Rewrite action node was not found in $targetConfigPath"
}

$rewriteAction.SetAttribute("url", "http://127.0.0.1:$BackendPort/{R:1}")
$webConfig.Save($targetConfigPath)

Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" -Filter "system.webServer/proxy" -Name "enabled" -Value "True"

if (-not (Test-Path "IIS:\Sites\$SiteName"))
{
    New-Website -Name $SiteName -Port 80 -HostHeader $HostName -PhysicalPath $resolvedPhysicalPath | Out-Null
}
else
{
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $resolvedPhysicalPath
}

if (-not (Get-WebBinding -Name $SiteName -Protocol "https" -ErrorAction SilentlyContinue | Where-Object { $_.bindingInformation -like "*:443:$HostName" }))
{
    New-WebBinding -Name $SiteName -Protocol "https" -Port 443 -HostHeader $HostName | Out-Null
}

$httpsBindingPath = "IIS:\SslBindings\0.0.0.0!443!$HostName"
if (Test-Path $httpsBindingPath)
{
    Remove-Item $httpsBindingPath -Force
}

New-Item $httpsBindingPath -Thumbprint $CertificateThumbprint -SSLFlags 1 | Out-Null

Write-Host "Configured IIS reverse proxy:"
Write-Host "  SiteName     : $SiteName"
Write-Host "  HostName     : $HostName"
Write-Host "  PhysicalPath : $resolvedPhysicalPath"
Write-Host "  Backend      : http://127.0.0.1:$BackendPort"
Write-Host "  WebConfig    : $targetConfigPath"
