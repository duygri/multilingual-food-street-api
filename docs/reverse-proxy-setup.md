# Reverse Proxy Setup

Use these samples when you deploy `NarrationApp.Server` behind a public HTTPS domain such as `https://narration.app/`.

The app now accepts the standard forwarded headers:

- `X-Forwarded-For`
- `X-Forwarded-Proto`
- `X-Forwarded-Host`

That means the reverse proxy should preserve the real host and HTTPS scheme when forwarding traffic to Kestrel on port `5000`.

## Nginx

Sample file:

- [deploy/reverse-proxy/nginx/narrationapp.conf](D:/VinhKhanhFoodStreet/deploy/reverse-proxy/nginx/narrationapp.conf)

Typical Linux flow:

1. Copy the sample to `/etc/nginx/sites-available/narrationapp.conf`
2. Adjust:
   - `server_name`
   - `ssl_certificate`
   - `ssl_certificate_key`
3. Enable it:

```bash
sudo ln -s /etc/nginx/sites-available/narrationapp.conf /etc/nginx/sites-enabled/narrationapp.conf
sudo nginx -t
sudo systemctl reload nginx
```

The sample already forwards:

- normal HTTP traffic
- SignalR websocket upgrades
- host and HTTPS headers required by the app

## IIS

Sample file:

- [deploy/reverse-proxy/iis/web.config](D:/VinhKhanhFoodStreet/deploy/reverse-proxy/iis/web.config)
- helper script: [scripts/configure_iis_reverse_proxy.ps1](D:/VinhKhanhFoodStreet/scripts/configure_iis_reverse_proxy.ps1)

Use this when IIS is only acting as the HTTPS reverse proxy in front of the helper-installed Kestrel app at `http://127.0.0.1:5000`.

Requirements:

1. Install `URL Rewrite`
2. Install `Application Request Routing (ARR)`
3. In IIS Manager, enable `Proxy` under ARR server settings
4. Bind the site to the real HTTPS certificate for `narration.app`

Before those IIS extras, install the built-in IIS features and PowerShell tooling.

On Windows Server:

```powershell
Install-WindowsFeature Web-Server, Web-Mgmt-Tools, Web-Scripting-Tools
```

On Windows 10/11:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-ManagementScriptingTools, IIS-ManagementConsole -All
```

Or use the helper script, which auto-detects Windows Server vs Windows 10/11:

- [scripts/install_iis_prerequisites.ps1](D:/VinhKhanhFoodStreet/scripts/install_iis_prerequisites.ps1)

After that:

1. Create an IIS site for `narration.app`
2. Point the site root to a folder that contains the sample `web.config`
3. Restart the IIS site

Or use the helper script directly on the Windows server:

```powershell
powershell -ExecutionPolicy Bypass -File .\configure_iis_reverse_proxy.ps1 `
  -SiteName "narration.app" `
  -HostName "narration.app" `
  -PhysicalPath "C:\inetpub\narrationapp-proxy" `
  -CertificateThumbprint "YOUR_CERT_THUMBPRINT" `
  -BackendPort 5000
```

## Runtime checks

After the reverse proxy is live, verify:

1. `https://narration.app/.well-known/assetlinks.json`
2. `https://narration.app/qr/<sample-code>`
3. SignalR notification traffic still works in admin/owner web
4. Android app links open the app when the signed package is installed

If the public site loads but QR/app-link URLs show the wrong scheme or host, the proxy is not forwarding the headers above correctly.
