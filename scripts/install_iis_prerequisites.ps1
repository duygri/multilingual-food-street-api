param()

$ErrorActionPreference = "Stop"

Write-Host "Installing IIS prerequisites..."

if (Get-Module -ListAvailable -Name ServerManager)
{
    Import-Module ServerManager

    $features = @(
        "Web-Server",
        "Web-Mgmt-Tools",
        "Web-Scripting-Tools"
    )

    Write-Host "  Detected Windows Server feature management."
    Write-Host ("  Features: " + ($features -join ", "))

    $result = Install-WindowsFeature Web-Server, Web-Mgmt-Tools, Web-Scripting-Tools
    if (-not $result.Success)
    {
        throw "Failed to install one or more IIS prerequisites."
    }
}
elseif (Get-Command Enable-WindowsOptionalFeature -ErrorAction SilentlyContinue)
{
    $features = @(
        "IIS-WebServerRole",
        "IIS-ManagementConsole",
        "IIS-ManagementScriptingTools"
    )

    Write-Host "  Detected Windows 10/11 optional feature management."
    Write-Host ("  Features: " + ($features -join ", "))

    Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-ManagementScriptingTools, IIS-ManagementConsole -All
}
else
{
    throw @"
Neither ServerManager nor Enable-WindowsOptionalFeature is available.

If this is Windows Server, install IIS through Server Manager.
If this is Windows 10/11, enable IIS and IIS Management Scripts and Tools from Windows Features.
"@
}

Write-Host "Installed IIS prerequisites successfully."
Write-Host "Next manual installs still required:"
Write-Host "  1. URL Rewrite"
Write-Host "  2. Application Request Routing (ARR)"
