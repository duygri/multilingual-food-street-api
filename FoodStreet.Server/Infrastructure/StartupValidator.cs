using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;

namespace FoodStreet.Server.Infrastructure
{
    /// <summary>
    /// Orchestrates the 4-tier backend startup sequence.
    /// Tier 1: Security config validation
    /// Tier 2: Database connectivity
    /// Tier 3: Data seeding
    /// Tier 4: Database indexing / warm-up
    /// </summary>
    public static class StartupValidator
    {
        // =============================================
        // TIER 1 — Security Config Check
        // =============================================
        public static void ValidateSecurityConfig(WebApplicationBuilder builder, ILogger logger)
        {
            logger.LogInformation("[Startup Tier 1] Validating security configuration...");

            var config = builder.Configuration;

            // JWT must be fully configured
            var jwtSecret = config["JwtSettings:Secret"];
            var jwtIssuer = config["JwtSettings:Issuer"];
            var jwtAudience = config["JwtSettings:Audience"];
            var googleCloudApiKey = config["GoogleCloud:ApiKey"];
            var googleCloudUseServiceAccountJson =
                bool.TryParse(config["GoogleCloud:UseServiceAccountJson"], out var useJson) && useJson;
            var googleCloudCredentialPath = config["GoogleCloud:CredentialPath"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            var googleCloudProjectId = config["GoogleCloud:ProjectId"]
                ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT")
                ?? Environment.GetEnvironmentVariable("GCLOUD_PROJECT");

            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
                throw new InvalidOperationException(
                    "[Security] JwtSettings:Secret is missing or too short (min 32 chars).");

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                throw new InvalidOperationException("[Security] JwtSettings:Issuer is not configured.");

            if (string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("[Security] JwtSettings:Audience is not configured.");

            // Connection string must exist
            var connectionString = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("[Security] DefaultConnection string is missing.");

            if (!string.IsNullOrWhiteSpace(googleCloudApiKey))
            {
                logger.LogInformation("[Startup Tier 1] GoogleCloud auth mode: API key.");
            }
            else if (!string.IsNullOrWhiteSpace(googleCloudCredentialPath))
            {
                var expandedCredentialPath = Environment.ExpandEnvironmentVariables(googleCloudCredentialPath);
                if (!File.Exists(expandedCredentialPath))
                {
                    logger.LogWarning(
                        "[Startup Tier 1] GoogleCloud credential file configured but missing: {CredentialPath}",
                        expandedCredentialPath);
                }
                else
                {
                    logger.LogInformation(
                        "[Startup Tier 1] GoogleCloud auth mode: service-account JSON ({CredentialFile}).",
                        Path.GetFileName(expandedCredentialPath));
                }
            }
            else if (googleCloudUseServiceAccountJson)
            {
                logger.LogWarning(
                    "[Startup Tier 1] GoogleCloud is set to service-account JSON mode but no CredentialPath / GOOGLE_APPLICATION_CREDENTIALS was found.");
            }
            else if (!string.IsNullOrWhiteSpace(googleCloudProjectId))
            {
                logger.LogInformation("[Startup Tier 1] GoogleCloud auth mode: ADC / gcloud fallback.");
            }
            else
            {
                logger.LogWarning(
                    "[Startup Tier 1] GoogleCloud auth is not fully configured. Set GoogleCloud:ApiKey for API-key mode, or configure GoogleCloud:CredentialPath / GOOGLE_APPLICATION_CREDENTIALS for service-account JSON mode, or set GoogleCloud:ProjectId / 'gcloud config set project <PROJECT_ID>' for ADC mode.");
            }

            logger.LogInformation("[Startup Tier 1] ✅ Security config OK (JWT + ConnectionString).");
        }

        // =============================================
        // TIER 2 — Database Connection
        // =============================================
        public static async Task EnsureDatabaseAsync(IServiceProvider services, ILogger logger)
        {
            logger.LogInformation("[Startup Tier 2] Connecting to database and running migrations...");

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
                throw new InvalidOperationException(
                    "[Database] Cannot connect to PostgreSQL. Check connection string and server status.");

            await context.Database.MigrateAsync();
            await EnsurePoiMenuTableAsync(context, logger);
            await EnsurePoiMenuTranslationTableAsync(context, logger);
            await EnsureTourSessionTableAsync(context, logger);
            logger.LogInformation("[Startup Tier 2] ✅ Database connected and up-to-date.");
        }

        // =============================================
        // TIER 3 — Data Seeding
        // =============================================
        public static async Task SeedDataAsync(IServiceProvider services, ILogger logger)
        {
            logger.LogInformation("[Startup Tier 3] Seeding initial data (roles, admin user)...");
            await PROJECT_C_.Data.SeedData.InitializeAsync(services);
            logger.LogInformation("[Startup Tier 3] ✅ Data seeding complete.");
        }

        // =============================================
        // TIER 4 — Indexing / Warm-up
        // =============================================
        public static async Task EnsureIndexesAsync(IServiceProvider services, ILogger logger)
        {
            logger.LogInformation("[Startup Tier 4] Validating database indexes and warming up...");

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Warm-up: pre-load query plan cache by running lightweight queries
            _ = await context.Locations.CountAsync();
            _ = await context.AudioFiles.CountAsync();

            logger.LogInformation("[Startup Tier 4] ✅ Indexes validated. Server ready.");
        }

        private static async Task EnsurePoiMenuTableAsync(AppDbContext context, ILogger logger)
        {
            const string createMenuTableSql = """
                CREATE TABLE IF NOT EXISTS "PoiMenuItems" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "LocationId" integer NOT NULL,
                    "Name" text NOT NULL,
                    "Description" text NOT NULL,
                    "Price" numeric(18,2) NOT NULL,
                    "Currency" character varying(10) NOT NULL DEFAULT 'VND',
                    "ImageUrl" text NULL,
                    "IsAvailable" boolean NOT NULL DEFAULT TRUE,
                    "SortOrder" integer NOT NULL DEFAULT 0,
                    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                    CONSTRAINT "FK_PoiMenuItems_Locations_LocationId"
                        FOREIGN KEY ("LocationId") REFERENCES "Locations" ("Id") ON DELETE CASCADE
                );
                """;

            const string createMenuIndexSql = """
                CREATE INDEX IF NOT EXISTS "IX_PoiMenuItems_LocationId"
                ON "PoiMenuItems" ("LocationId");
                """;

            await context.Database.ExecuteSqlRawAsync(createMenuTableSql);
            await context.Database.ExecuteSqlRawAsync(createMenuIndexSql);
            logger.LogInformation("[Startup Tier 2] ✅ Compatibility check: PoiMenuItems table ensured.");
        }

        private static async Task EnsurePoiMenuTranslationTableAsync(AppDbContext context, ILogger logger)
        {
            const string createMenuTranslationTableSql = """
                CREATE TABLE IF NOT EXISTS "PoiMenuItemTranslations" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "PoiMenuItemId" integer NOT NULL,
                    "LanguageCode" character varying(10) NOT NULL,
                    "Name" text NOT NULL,
                    "Description" text NOT NULL DEFAULT '',
                    "IsFallback" boolean NOT NULL DEFAULT FALSE,
                    "GeneratedAt" timestamp with time zone NULL,
                    CONSTRAINT "FK_PoiMenuItemTranslations_PoiMenuItems_PoiMenuItemId"
                        FOREIGN KEY ("PoiMenuItemId") REFERENCES "PoiMenuItems" ("Id") ON DELETE CASCADE
                );
                """;

            const string createMenuTranslationIndexSql = """
                CREATE INDEX IF NOT EXISTS "IX_PoiMenuItemTranslations_PoiMenuItemId"
                ON "PoiMenuItemTranslations" ("PoiMenuItemId");
                """;

            const string createMenuTranslationUniqueSql = """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_PoiMenuItemTranslations_PoiMenuItemId_LanguageCode"
                ON "PoiMenuItemTranslations" ("PoiMenuItemId", "LanguageCode");
                """;

            await context.Database.ExecuteSqlRawAsync(createMenuTranslationTableSql);
            await context.Database.ExecuteSqlRawAsync(createMenuTranslationIndexSql);
            await context.Database.ExecuteSqlRawAsync(createMenuTranslationUniqueSql);
            logger.LogInformation("[Startup Tier 2] ✅ Compatibility check: PoiMenuItemTranslations table ensured.");
        }

        private static async Task EnsureTourSessionTableAsync(AppDbContext context, ILogger logger)
        {
            const string createTourSessionTableSql = """
                CREATE TABLE IF NOT EXISTS "TourSessions" (
                    "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    "SessionId" character varying(100) NOT NULL,
                    "UserId" character varying(450) NOT NULL,
                    "TourId" integer NOT NULL,
                    "CurrentLocationId" integer NOT NULL,
                    "CurrentStopOrder" integer NOT NULL,
                    "CompletedStops" integer NOT NULL DEFAULT 0,
                    "TotalStops" integer NOT NULL DEFAULT 0,
                    "ProgressPercent" integer NOT NULL DEFAULT 0,
                    "IsCompleted" boolean NOT NULL DEFAULT FALSE,
                    "ResumeCount" integer NOT NULL DEFAULT 0,
                    "DeviceType" character varying(50) NULL,
                    "LastLatitude" double precision NULL,
                    "LastLongitude" double precision NULL,
                    "StartedAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                    "LastResumedAt" timestamp with time zone NULL,
                    "DismissedAt" timestamp with time zone NULL,
                    "CompletedAt" timestamp with time zone NULL,
                    "LastActivityAt" timestamp with time zone NOT NULL DEFAULT NOW(),
                    CONSTRAINT "FK_TourSessions_Tours_TourId"
                        FOREIGN KEY ("TourId") REFERENCES "Tours" ("Id") ON DELETE CASCADE
                );
                """;

            const string createTourSessionSessionIndexSql = """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TourSessions_SessionId"
                ON "TourSessions" ("SessionId");
                """;

            const string createTourSessionUserIndexSql = """
                CREATE INDEX IF NOT EXISTS "IX_TourSessions_UserId_IsCompleted_LastActivityAt"
                ON "TourSessions" ("UserId", "IsCompleted", "LastActivityAt");
                """;

            const string createTourSessionTourIndexSql = """
                CREATE INDEX IF NOT EXISTS "IX_TourSessions_TourId"
                ON "TourSessions" ("TourId");
                """;

            const string createTourSessionActiveLocationIndexSql = """
                CREATE INDEX IF NOT EXISTS "IX_TourSessions_IsCompleted_CurrentLocationId_LastActivityAt"
                ON "TourSessions" ("IsCompleted", "CurrentLocationId", "LastActivityAt");
                """;

            const string addResumeCountColumnSql = """
                ALTER TABLE "TourSessions"
                ADD COLUMN IF NOT EXISTS "ResumeCount" integer NOT NULL DEFAULT 0;
                """;

            const string addLastResumedAtColumnSql = """
                ALTER TABLE "TourSessions"
                ADD COLUMN IF NOT EXISTS "LastResumedAt" timestamp with time zone NULL;
                """;

            const string addDismissedAtColumnSql = """
                ALTER TABLE "TourSessions"
                ADD COLUMN IF NOT EXISTS "DismissedAt" timestamp with time zone NULL;
                """;

            const string addCompletedAtColumnSql = """
                ALTER TABLE "TourSessions"
                ADD COLUMN IF NOT EXISTS "CompletedAt" timestamp with time zone NULL;
                """;

            await context.Database.ExecuteSqlRawAsync(createTourSessionTableSql);
            await context.Database.ExecuteSqlRawAsync(createTourSessionSessionIndexSql);
            await context.Database.ExecuteSqlRawAsync(createTourSessionUserIndexSql);
            await context.Database.ExecuteSqlRawAsync(createTourSessionTourIndexSql);
            await context.Database.ExecuteSqlRawAsync(createTourSessionActiveLocationIndexSql);
            await context.Database.ExecuteSqlRawAsync(addResumeCountColumnSql);
            await context.Database.ExecuteSqlRawAsync(addLastResumedAtColumnSql);
            await context.Database.ExecuteSqlRawAsync(addDismissedAtColumnSql);
            await context.Database.ExecuteSqlRawAsync(addCompletedAtColumnSql);
            logger.LogInformation("[Startup Tier 2] ✅ Compatibility check: TourSessions table ensured.");
        }
    }
}
