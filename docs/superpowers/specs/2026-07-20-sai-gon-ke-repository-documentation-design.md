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
6. **Prerequisites** — .NET 8 SDK, .NET 9 SDK plus Android workload for the mobile app, PostgreSQL, and optional Mapbox/Google/Cloudflare services.
7. **Local setup** — copy-safe commands for restoring, starting the API, starting the Web app, and running tests; configuration is described without embedding secrets.
8. **Configuration and security** — point to appsettings examples, explain local overrides, and explicitly warn against committing access tokens, JWT signing keys, database passwords, or cloud credentials.
9. **Testing and validation** — identify the web/server test projects and the expected command shape without inventing a CI badge or unsupported coverage claim.
10. **Documentation and status** — link to the relevant docs and describe the current city-scale frontend work and remaining roadmap areas.
11. **English/Vietnamese project notes** — short bilingual contributor and product notes, keeping commands and code identifiers unchanged.

## GitHub About metadata

Use this concise description:

> Citywide audio walking tours for Ho Chi Minh City: POI discovery, multilingual narration, QR entry, maps, and owner analytics.

Add topics that match the actual stack and product: `dotnet`, `aspnet-core`, `blazor`, `maui`, `android`, `postgresql`, `mapbox`, `qr-code`, `audio-guide`, `ho-chi-minh-city`, and `vietnam`. Leave the website field empty until a verified public demo or production URL exists.

## Acceptance criteria

- A root `README.md` exists and is written in the approved bilingual format.
- Setup commands and framework versions match the checked-in projects.
- No secret values or fabricated URLs appear in the README.
- All referenced paths exist or are clearly marked as optional/future work.
- GitHub About has the approved description and topics, with no unverified website link.
- Existing user-owned modifications in `src/NarrationApp.Mobile/Components/Pages/Home.razor.cs`, `.superpowers/`, and `tmp/` remain untouched.

