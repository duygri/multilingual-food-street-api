[CmdletBinding()]
param(
    [string]$WebUrl = "http://127.0.0.1:5100",
    [string]$ApiUrl = "https://localhost:5001",
    [string]$ServerProject = "D:\VinhKhanhFoodStreet\src\NarrationApp.Server\NarrationApp.Server.csproj",
    [string]$WebProject = "D:\VinhKhanhFoodStreet\src\NarrationApp.Web\NarrationApp.Web.csproj"
)

$ErrorActionPreference = "Stop"

function Get-BrowserPath {
    $candidates = @(
        "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        "C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        "C:\Program Files\Google\Chrome\Application\chrome.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "No supported browser executable was found."
}

function Wait-HttpReady {
    param(
        [string]$Url,
        [switch]$SkipCertificateCheck,
        [int]$TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            if ($SkipCertificateCheck) {
                Invoke-WebRequest -Uri $Url -SkipCertificateCheck -TimeoutSec 5 | Out-Null
            }
            else {
                Invoke-WebRequest -Uri $Url -TimeoutSec 5 | Out-Null
            }

            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Timed out waiting for $Url"
}

function Read-CdpMessage {
    param([System.Net.WebSockets.ClientWebSocket]$Socket)

    $buffer = New-Object byte[] 4096
    $stream = New-Object System.IO.MemoryStream

    do {
        $segment = [ArraySegment[byte]]::new($buffer)
        $result = $Socket.ReceiveAsync($segment, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

        if ($result.MessageType -eq [System.Net.WebSockets.WebSocketMessageType]::Close) {
            throw "Browser DevTools socket was closed unexpectedly."
        }

        $stream.Write($buffer, 0, $result.Count)
    } until ($result.EndOfMessage)

    $json = [Text.Encoding]::UTF8.GetString($stream.ToArray())
    return $json | ConvertFrom-Json -Depth 100
}

$script:CdpMessageId = 0
function Send-Cdp {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [string]$Method,
        [hashtable]$Params = @{}
    )

    $messageId = [Threading.Interlocked]::Increment([ref]$script:CdpMessageId)
    $payload = @{
        id     = $messageId
        method = $Method
    }

    if ($Params.Count -gt 0) {
        $payload.params = $Params
    }

    $json = $payload | ConvertTo-Json -Compress -Depth 100
    $bytes = [Text.Encoding]::UTF8.GetBytes($json)
    $segment = [ArraySegment[byte]]::new($bytes)
    $Socket.SendAsync($segment, [System.Net.WebSockets.WebSocketMessageType]::Text, $true, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    while ($true) {
        $message = Read-CdpMessage -Socket $Socket
        if ($null -ne $message.id -and [int]$message.id -eq $messageId) {
            return $message
        }
    }
}

function Get-BodyText {
    param([System.Net.WebSockets.ClientWebSocket]$Socket)

    $result = Send-Cdp -Socket $Socket -Method "Runtime.evaluate" -Params @{
        expression    = "document.body ? document.body.innerText : ''"
        returnByValue = $true
    }

    return [string]$result.result.result.value
}

function Wait-ForBodyText {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [string[]]$ExpectedTexts,
        [int]$TimeoutSeconds = 20
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastText = ""
    while ((Get-Date) -lt $deadline) {
        $lastText = Get-BodyText -Socket $Socket
        $allMatched = $true
        foreach ($expected in $ExpectedTexts) {
            if ($lastText -notlike "*$expected*") {
                $allMatched = $false
                break
            }
        }

        if ($allMatched) {
            return $lastText
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for page text: $($ExpectedTexts -join ', '). Last text: $lastText"
}

function Navigate-ToUrl {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [string]$Url
    )

    Send-Cdp -Socket $Socket -Method "Page.navigate" -Params @{ url = $Url } | Out-Null
}

function Set-SessionAndNavigate {
    param(
        [System.Net.WebSockets.ClientWebSocket]$Socket,
        [hashtable]$Session,
        [string]$TargetUrl
    )

    $sessionJson = $Session | ConvertTo-Json -Compress -Depth 20
    $sessionLiteral = $sessionJson | ConvertTo-Json -Compress
    $expression = "localStorage.setItem('narration-app.auth-session', $sessionLiteral); window.location.href = '$TargetUrl'; 'ok';"

    Send-Cdp -Socket $Socket -Method "Runtime.evaluate" -Params @{
        expression    = $expression
        returnByValue = $true
    } | Out-Null
}

function New-AuthSession {
    param([pscustomobject]$AuthResponse)

    return [ordered]@{
        userId            = $AuthResponse.userId
        email             = $AuthResponse.email
        role              = $AuthResponse.role
        preferredLanguage = $AuthResponse.preferredLanguage
        token             = $AuthResponse.token
    }
}

$workspaceRoot = Split-Path -Path $PSScriptRoot -Parent
$solutionPath = Join-Path $workspaceRoot "NarrationApp.sln"
$smokeDir = Join-Path $workspaceRoot ".artifacts\smoke"
$browserDataDir = Join-Path $smokeDir "edge-profile"
$serverStdOut = Join-Path $smokeDir "server.out.log"
$serverStdErr = Join-Path $smokeDir "server.err.log"
$webStdOut = Join-Path $smokeDir "web.out.log"
$webStdErr = Join-Path $smokeDir "web.err.log"

New-Item -ItemType Directory -Force -Path $smokeDir | Out-Null
New-Item -ItemType Directory -Force -Path $browserDataDir | Out-Null

$browserPath = Get-BrowserPath
$serverProcess = $null
$webProcess = $null
$browserProcess = $null
$socket = $null

try {
    & dotnet build $solutionPath --no-restore -m:1 -p:UseSharedCompilation=false -v minimal | Out-Null

    $serverProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
        "run",
        "--project", $ServerProject,
        "--no-launch-profile",
        "--no-restore",
        "--no-build",
        "--urls", "https://localhost:5001;http://localhost:5000"
    ) -RedirectStandardOutput $serverStdOut -RedirectStandardError $serverStdErr -PassThru

    $webProcess = Start-Process -FilePath "dotnet" -ArgumentList @(
        "run",
        "--project", $WebProject,
        "--no-launch-profile",
        "--no-restore",
        "--no-build",
        "--urls", $WebUrl
    ) -RedirectStandardOutput $webStdOut -RedirectStandardError $webStdErr -PassThru

    Wait-HttpReady -Url "$ApiUrl/api/health" -SkipCertificateCheck
    Wait-HttpReady -Url "$WebUrl/auth/login"

    $adminEnvelope = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -SkipCertificateCheck -Method Post -ContentType "application/json" -Body (@{
        email    = "admin@narration.app"
        password = "Admin@123"
    } | ConvertTo-Json)
    $ownerEnvelope = Invoke-RestMethod -Uri "$ApiUrl/api/auth/login" -SkipCertificateCheck -Method Post -ContentType "application/json" -Body (@{
        email    = "owner@narration.app"
        password = "Owner@123"
    } | ConvertTo-Json)

    $browserProcess = Start-Process -FilePath $browserPath -ArgumentList @(
        "--headless=new",
        "--disable-gpu",
        "--ignore-certificate-errors",
        "--remote-debugging-port=9222",
        "--user-data-dir=$browserDataDir",
        "about:blank"
    ) -PassThru

    Wait-HttpReady -Url "http://127.0.0.1:9222/json/version"
    $target = Invoke-RestMethod -Uri ("http://127.0.0.1:9222/json/new?{0}" -f [uri]::EscapeDataString("$WebUrl/auth/login")) -Method Put

    $socket = [System.Net.WebSockets.ClientWebSocket]::new()
    $socket.ConnectAsync([Uri]$target.webSocketDebuggerUrl, [Threading.CancellationToken]::None).GetAwaiter().GetResult()

    Send-Cdp -Socket $socket -Method "Page.enable" | Out-Null
    Send-Cdp -Socket $socket -Method "Runtime.enable" | Out-Null

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/auth/login"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Đăng nhập vào Narration Portal", "Portal access") | Out-Null
    Write-Host "[Smoke] Login page rendered."

    $adminSession = New-AuthSession -AuthResponse $adminEnvelope.data
    Set-SessionAndNavigate -Socket $socket -Session $adminSession -TargetUrl "$WebUrl/admin/dashboard"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Tuyến ưu tiên của ca trực", "Người dùng đang hoạt động") | Out-Null
    Write-Host "[Smoke] Admin dashboard rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/poi-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("POI toàn hệ thống", "Moderation & actions") | Out-Null
    Write-Host "[Smoke] Admin POI management rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/analytics"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Sàn tín hiệu thời gian thực", "Làn nhiệt POI") | Out-Null
    Write-Host "[Smoke] Admin analytics rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/moderation-queue"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Tuyến xử lý moderation", "Bàn phân luồng") | Out-Null
    Write-Host "[Smoke] Admin moderation queue rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/user-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Trung tâm quyền truy cập", "Tình trạng hiện diện") | Out-Null
    Write-Host "[Smoke] Admin user management rendered."

    Set-SessionAndNavigate -Socket $socket -Session $adminSession -TargetUrl "$WebUrl/admin/tour-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Tour editor", "Tạo tour mới") | Out-Null
    Write-Host "[Smoke] Admin tour management rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/category-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Danh mục", "Tạo danh mục mới") | Out-Null
    Write-Host "[Smoke] Admin category management rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/qr-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Tạo mã QR", "Danh sách QR") | Out-Null
    Write-Host "[Smoke] Admin QR management rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/audio-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Kịch bản nguồn tiếng Việt", "Bàn chất lượng asset") | Out-Null
    Write-Host "[Smoke] Admin audio management rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/admin/translation-review"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Lane kiểm định bản dịch", "Bảng vùng phủ ngôn ngữ") | Out-Null
    Write-Host "[Smoke] Admin translation review rendered."

    $ownerSession = New-AuthSession -AuthResponse $ownerEnvelope.data
    Set-SessionAndNavigate -Socket $socket -Session $ownerSession -TargetUrl "$WebUrl/owner/dashboard"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Bảng sẵn sàng POI", "Theo dõi kiểm duyệt") | Out-Null
    Write-Host "[Smoke] Owner dashboard rendered."

    Navigate-ToUrl -Socket $socket -Url "$WebUrl/owner/poi-management"
    Wait-ForBodyText -Socket $socket -ExpectedTexts @("Bàn điều phối POI", "Script TTS tiếng Việt") | Out-Null
    Write-Host "[Smoke] Owner POI management rendered."

    Write-Host "[Smoke] PASS"
}
finally {
    if ($socket) {
        $socket.Dispose()
    }

    foreach ($process in @($browserProcess, $webProcess, $serverProcess)) {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }
}
