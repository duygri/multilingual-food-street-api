# Public QR Domain

For internet-facing QR codes, use the server's real HTTPS public domain as the canonical QR host.

## Canonical URL shape

- `https://<public-server-domain>/qr/{code}`

## Server configuration

Set `PublicQr:BaseUrl` on `NarrationApp.Server` to the public HTTPS base URL of the deployed server.

Examples:

```json
{
  "PublicQr": {
    "BaseUrl": "https://api.foodstreet.vn/"
  }
}
```

Environment variable form:

```text
PublicQr__BaseUrl=https://api.foodstreet.vn/
```

Notes:

- Always use the externally reachable HTTPS domain, not `localhost`, not a LAN IP, and not an internal container hostname.
- Keep the trailing slash or let the app normalize it.
- The `/qr/{code}` landing page is served by `NarrationApp.Server`.

## Web admin preview

If the admin web app is hosted on the same public origin as the server, `QrPublicBaseUrl` can stay relative as `/`.

If the admin web app is hosted on a different origin, set `QrPublicBaseUrl` to the same public QR domain so preview links and generated QR previews match production:

```json
{
  "QrPublicBaseUrl": "https://api.foodstreet.vn/"
}
```

## Deployment checklist

- Deploy `NarrationApp.Server` behind a real HTTPS public domain.
- Set `PublicQr:BaseUrl` to that public domain.
- Confirm `https://<public-server-domain>/qr/<sample-code>` opens the QR landing page.
- Confirm `GET /api/qr/{code}` returns `publicUrl` using the same domain.
- If admin web runs elsewhere, set `QrPublicBaseUrl` to the same public domain.
