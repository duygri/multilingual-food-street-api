# Sài Gòn Kể Repository Documentation Design

## Context

The repository currently has no root-level `README.md`; only the mobile font asset folder contains a local README. The product has moved from a Vĩnh Khánh-focused identity to Sài Gòn Kể, a citywide Ho Chi Minh City visitor experience. GitHub's repository About section is also empty, so the project lacks a concise public description, searchable topics, and an onboarding path for contributors.

## Goals

- Give visitors and contributors an accurate bilingual overview of Sài Gòn Kể.
- Explain the repository's three runtime surfaces: ASP.NET Core API/server, Blazor Web owner/admin portal, and .NET MAUI Android visitor app.
- Document the supported local-development workflow without exposing credentials or implying unavailable production services.
- Make the GitHub repository discoverable with a concise About description and relevant topics.
- Keep the README synchronized with the citywide product direction and current implementation status.

## Non-goals

- Changing application behavior or runtime configuration.
- Replacing the detailed design and deployment documents already under `docs/`.
- Adding real secrets, production URLs, screenshots, or claims that cannot be verified from the repository.
- Deleting or rewriting user-owned worktree files.

## Proposed README structure

1. **Bilingual title and summary** — English-first product statement followed by a Vietnamese explanation.
2. **Product scope** — citywide Ho Chi Minh City discovery, multilingual narration, POIs, QR entry, maps, journeys, and analytics.
3. **What is included** — API/server, Web owner/admin portal, and Android visitor app with their responsibilities.
4. **Key capabilities** — visitor discovery, audio and translation workflows, QR flows, notifications, owner/admin operations, and analytics.
5. **Architecture and repository map** — concise mapping of `src/`, `tests/`, `docs/`, `deploy/`, and `scripts/`.
6. **Prerequisites** — the repository's `global.json` pins .NET SDK `9.0.312`; document that the SDK can build the net8.0 API/Web/test projects and the net9.0 Android project, and call out the required MAUI Android workload for mobile builds. PostgreSQL and optional Mapbox/Google/Cloudflare services are listed separately.
7. **Local setup** — copy-safe commands for restoring, starting the API, starting the Web app, running both test projects, and checking/installing the MAUI Android workload. Document that the mobile app needs a local `visitor-api.json` endpoint configuration (using the checked-in sample/template path where available) instead of embedding a machine-specific URL. State that the checked-in sample contains placeholder/example endpoints and must be replaced for a real device or release build.
8. **Configuration and security** — point to appsettings examples, explain local overrides and .NET user-secrets/environment variables, and explicitly warn that checked-in development settings contain placeholder-only values such as `Password=123456` and a development JWT signing key. Never reuse those values outside local development and never commit real access tokens, JWT signing keys, database passwords, or cloud credentials.
9. **Testing and validation** — specify `dotnet test tests/NarrationApp.Server.Tests/NarrationApp.Server.Tests.csproj` and `dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj`, a server/Web build check, and an Android workload/build check. Explain that server integration-style tests replace PostgreSQL with EF Core InMemory and the Web tests are local component tests, so a running PostgreSQL instance is needed for the API itself but not for the standard test commands. Do not invent a CI badge or unsupported coverage claim.
10. **Documentation and status** — link to the relevant docs and describe the current city-scale frontend work and remaining roadmap areas.
11. **English/Vietnamese project notes** — short bilingual contributor and product notes, keeping commands and code identifiers unchanged.
12. **Current-scope caveat** — clearly state, in both language versions near the top, that the Sài Gòn Kể citywide frontend direction is in progress while analytics bounds and some seeded/portal content remain District 4/Vĩnh Khánh-scoped until the remaining city-scale work is completed. Clarify that this currently limits the dashboard/data interpretation rather than claiming that every visitor flow is restricted to one district.

## Verified runtime fallbacks and build constraints

- Without a Mapbox token, the Web analytics map and mobile map/directions surfaces report an unavailable state; POI/audio/QR flows can still be documented separately where they do not require Mapbox.
- Without Google Cloud credentials, the server registers mock translation and text-to-speech services for local/test use.
- Without Cloudflare R2 credentials, the server uses local mock storage under its local `wwwroot/audio` path.
- The Android project targets `net9.0-android35.0` and supports Android API 24+ at runtime. The README should require the MAUI Android workload and Android SDK platform 35, but must not invent a JDK version that is not pinned in the repository. Staging/Release builds require a real `VisitorApiConfigFile`; signed Staging/Release packages additionally require signing configuration, while unsigned builds remain possible for local validation.
- Before implementation is considered complete, run the documented restore, API/Web build, two test commands, and Debug Android build/workload check on the available environment; record any environment-blocked command honestly.

## GitHub About metadata

Use this concise, non-overclaiming description:

> Expanding multilingual audio walking tours across Ho Chi Minh City: POI discovery, QR entry, maps, and owner analytics.

Add topics that match the actual stack and product: `dotnet`, `aspnet-core`, `blazor`, `maui`, `android`, `postgresql`, `mapbox`, `qr-code`, `audio-guide`, `ho-chi-minh-city`, and `vietnam`. Leave the website field empty until a verified public demo or production URL exists. After saving, verify the exact description, topics, and empty website in the repository's GitHub About panel (or the repository metadata API if available).

## Acceptance criteria

- A root `README.md` exists and is written in the approved bilingual format.
- Setup commands and framework versions match `global.json` and the checked-in projects; the README includes exact test commands, build commands, MAUI workload/API-35 guidance, mobile endpoint configuration guidance, and provider fallback behavior.
- No secret values or fabricated URLs appear in the README.
- The README warns about the checked-in development-only credentials and records the current District 4/Vĩnh Khánh scope caveat in both languages near the top. If any development credential was reused outside local development, it must be rotated before publication; documentation alone is not a remediation.
- All referenced paths exist or are clearly marked as optional/future work.
- GitHub About has the approved non-overclaiming description and topics, with no unverified website link, and the saved metadata is verified in the GitHub UI/API.
- The documented commands are actually attempted before completion; tests use their documented in-memory/local dependencies and any unavailable Android/provider environment is reported rather than hidden.
- Existing user-owned modifications in `src/NarrationApp.Mobile/Components/Pages/Home.razor.cs`, `.superpowers/`, and `tmp/` remain untouched.
