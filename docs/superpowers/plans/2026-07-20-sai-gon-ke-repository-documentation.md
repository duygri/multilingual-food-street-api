# Sài Gòn Kể Repository Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an accurate bilingual root README and synchronize the GitHub About description/topics with the current Sài Gòn Kể product scope.

**Architecture:** Documentation-only repository changes plus a GitHub metadata update. The README will be English/Vietnamese paired sections, with a prominent scope caveat, verified local commands, explicit provider fallbacks, and no public secrets or fabricated URLs. Existing source and user-owned worktree files remain untouched.

**Tech Stack:** Markdown, .NET SDK `9.0.312`, ASP.NET Core/Blazor Web (`net8.0`), .NET MAUI Android (`net9.0-android35.0`), PostgreSQL, GitHub repository settings.

---

### Task 1: Build the bilingual root README

**Files:**
- Create: `README.md`
- Reference: `global.json`, `src/NarrationApp.Server/NarrationApp.Server.csproj`, `src/NarrationApp.Web/NarrationApp.Web.csproj`, `src/NarrationApp.Mobile/NarrationApp.Mobile.csproj`, `src/NarrationApp.Mobile/Resources/Raw/visitor-api.json`
- Reference: `docs/reverse-proxy-setup.md`, `docs/server-production-deploy.md`, `docs/public-qr-domain.md`, `docs/superpowers/specs/2026-07-20-sai-gon-ke-citywide-frontend-design.md`

- [ ] **Step 1: Write the bilingual product header and current-scope warning**

  Use the approved non-overclaiming wording: “Expanding multilingual audio walking tours across Ho Chi Minh City…”. Place matching English and Vietnamese caveats near the top: the citywide frontend direction is in progress, while analytics bounds and some seeded/portal content remain District 4/Vĩnh Khánh-scoped. Explain that this affects dashboard/data interpretation and does not claim every visitor flow is limited to one district.

- [ ] **Step 2: Document surfaces, capabilities, architecture, and repository map**

  Describe the ASP.NET Core API/server, Blazor Web owner/admin portal, and .NET MAUI Android visitor app. Map `src/`, `tests/`, `docs/`, `deploy/`, and `scripts/` to their responsibilities. Mention POI discovery, multilingual audio/translation, QR entry, journeys, notifications, maps, owner operations, and analytics without claiming completed citywide coverage.

- [ ] **Step 3: Add copy-safe prerequisites, setup, and configuration**

  Document the pinned SDK `9.0.312`, PostgreSQL for running the API, MAUI Android workload, Android SDK platform 35, and Android API 24+ runtime support. Include exact restore/run/build/test commands after checking them locally. Explain Web `appsettings.Development.json`/local overrides, server user-secrets/environment variables, and the mobile `visitor-api.json` sample. State that the sample contains example endpoints and must be replaced for a real device or release package.

- [ ] **Step 4: Add provider fallback and security notes**

  State verified behavior: missing Mapbox makes Web analytics map and mobile map/directions surfaces unavailable; missing Google credentials registers mock translation/TTS; missing Cloudflare R2 uses local mock storage. Warn that checked-in development settings contain development-only credential placeholders and a development JWT signing key; do not reproduce literal values in the README, reuse them outside local development, or commit real secrets. Mention that any reused credential must be rotated separately.

- [ ] **Step 5: Add testing, docs, roadmap, and bilingual contributor notes**

  Include the two exact test project commands, distinguish EF Core InMemory server tests/Web component tests from the PostgreSQL dependency of the running API, document Debug Android validation, the required `VisitorApiConfigFile` for Staging/Release, and signing-only variables (`ANDROID_KEYSTORE_PATH`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS`, `ANDROID_KEY_PASSWORD`) without exposing values. Link only to verified docs paths, and keep English/Vietnamese scope statements semantically identical.

- [ ] **Step 6: Review README for false claims and broken paths**

  Check every command, path, version, URL, provider fallback, and caveat against the repository. Ensure no access token, password, JWT key, machine-specific endpoint, or fabricated production URL appears in the new file.

- [ ] **Step 7: Commit the README**

  ```powershell
  git add -- README.md
  git commit -m "docs: add bilingual Sai Gon Ke repository guide"
  ```

### Task 2: Validate documented commands in the available environment

**Files:**
- Read-only validation of `global.json`, project files, and the newly created `README.md`

- [ ] **Step 1: Restore the solution**

  Run `dotnet restore NarrationApp.sln`. Expected: restore completes using SDK `9.0.312` or reports a documented environment limitation.

- [ ] **Step 2: Build server and Web projects**

  Run `dotnet build src/NarrationApp.Server/NarrationApp.Server.csproj --no-restore` and `dotnet build src/NarrationApp.Web/NarrationApp.Web.csproj --no-restore`. Expected: both succeed, or README records the exact environmental blocker.

- [ ] **Step 3: Run the two test projects**

  Run `dotnet test tests/NarrationApp.Server.Tests/NarrationApp.Server.Tests.csproj --no-restore` and `dotnet test tests/NarrationApp.Web.Tests/NarrationApp.Web.Tests.csproj --no-restore`. Expected: tests pass using their in-memory/local dependencies, or the README records the exact blocker without claiming success.

- [ ] **Step 4: Check MAUI workload and build Debug Android**

  Run `dotnet workload list`, verify Android SDK platform 35 is installed using the available SDK/workload tooling, then run `dotnet build src/NarrationApp.Mobile/NarrationApp.Mobile.csproj -f net9.0-android35.0 -c Debug --no-restore` when both are available. Expected: build succeeds, or the README records the exact missing workload/SDK condition. Do not attempt signed Staging/Release packaging without a real endpoint config and signing values.

- [ ] **Step 5: Update README if validation reveals drift**

  Correct only documentation claims that do not match observed output, then commit the documentation correction separately.

### Task 3: Update GitHub About metadata

**Files/Surfaces:**
- GitHub repository `duygri/multilingual-food-street-api` About panel
- Verify via repository page/settings after saving

- [ ] **Step 1: Set the description**

  Use: `Expanding multilingual audio walking tours across Ho Chi Minh City: POI discovery, QR entry, maps, and owner analytics.`

- [ ] **Step 2: Set verified topics**

  Add: `dotnet`, `aspnet-core`, `blazor`, `maui`, `android`, `postgresql`, `mapbox`, `qr-code`, `audio-guide`, `ho-chi-minh-city`, `vietnam`.

- [ ] **Step 3: Leave Website empty**

  Do not add a URL until a public demo or production URL is verified.

- [ ] **Step 4: Verify metadata**

  Confirm the exact description, all topics, empty website, and the sole default release branch in the GitHub UI. Record the resulting repository URL in the handoff.

### Task 4: Final review, commit, and push

**Files:**
- Review: `README.md`, `docs/superpowers/specs/2026-07-20-sai-gon-ke-repository-documentation-design.md`, `docs/superpowers/plans/2026-07-20-sai-gon-ke-repository-documentation.md`
- Preserve: `src/NarrationApp.Mobile/Components/Pages/Home.razor.cs`, `.superpowers/`, and `tmp/`

- [ ] **Step 1: Inspect the final diff and status**

  Run `git diff --check`, `git status --short`, and `git diff -- README.md`. Ensure only intended documentation files are staged.

- [ ] **Step 2: Commit any final documentation changes**

  ```powershell
  git add -- README.md
  git commit -m "docs: finalize Sai Gon Ke repository documentation"
  ```

- [ ] **Step 3: Push the documentation commit**

  Run `git push origin codex/vinh-khanh-release-polish` and verify the remote branch points to the new commit.

- [ ] **Step 4: Hand off evidence**

  Report the README path, GitHub repository URL, validation results, any environment-blocked checks, preserved user-owned changes, and the exact Git commit/push status.
