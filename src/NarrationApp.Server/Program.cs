using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NarrationApp.Server.Configuration;
using NarrationApp.Server.Data;
using NarrationApp.Server.Data.Seed;
using NarrationApp.Server.Extensions;
using NarrationApp.Server.Hubs;
using NarrationApp.Server.Middleware;
using NarrationApp.Server.Services;
using NarrationApp.Shared.Constants;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions =
        ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.ParentId;
});

var connectionString =
    builder.Configuration.GetConnectionString("PostgreSql")
    ?? "Host=localhost;Port=5432;Database=narration_app;Username=postgres;Password=123456";

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var rateLimitingOptions =
    builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
    ?? new RateLimitingOptions();
var requestDiagnosticsOptions =
    builder.Configuration.GetSection(RequestDiagnosticsOptions.SectionName).Get<RequestDiagnosticsOptions>()
    ?? new RequestDiagnosticsOptions();
var googleCloudOptions =
    builder.Configuration.GetSection(GoogleCloudOptions.SectionName).Get<GoogleCloudOptions>()
    ?? new GoogleCloudOptions();
var cloudflareR2Options =
    builder.Configuration.GetSection(CloudflareR2Options.SectionName).Get<CloudflareR2Options>()
    ?? new CloudflareR2Options();
var publicQrOptions =
    builder.Configuration.GetSection(PublicQrOptions.SectionName).Get<PublicQrOptions>()
    ?? new PublicQrOptions();
var mobileAppLinksOptions =
    builder.Configuration.GetSection(MobileAppLinksOptions.SectionName).Get<MobileAppLinksOptions>()
    ?? new MobileAppLinksOptions();
var visitEventRetentionOptions =
    builder.Configuration.GetSection(VisitEventRetentionOptions.SectionName).Get<VisitEventRetentionOptions>()
    ?? new VisitEventRetentionOptions();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection(RateLimitingOptions.SectionName));
builder.Services.Configure<RequestDiagnosticsOptions>(builder.Configuration.GetSection(RequestDiagnosticsOptions.SectionName));
builder.Services.Configure<GoogleCloudOptions>(builder.Configuration.GetSection(GoogleCloudOptions.SectionName));
builder.Services.Configure<CloudflareR2Options>(builder.Configuration.GetSection(CloudflareR2Options.SectionName));
builder.Services.Configure<PublicQrOptions>(builder.Configuration.GetSection(PublicQrOptions.SectionName));
builder.Services.Configure<MobileAppLinksOptions>(builder.Configuration.GetSection(MobileAppLinksOptions.SectionName));
builder.Services.Configure<VisitEventRetentionOptions>(builder.Configuration.GetSection(VisitEventRetentionOptions.SectionName));
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
});
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var correlationId = GetCorrelationId(context.HttpContext);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            context.ProblemDetails.Extensions["correlationId"] = correlationId;
        }
    };
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds)
                .ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteAsJsonAsync(
            CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status429TooManyRequests,
                "Too many requests.",
                "Rate limit exceeded. Please retry shortly."),
            cancellationToken: cancellationToken);
    };

    options.AddPolicy(
        AppConstants.AuthRateLimitPolicyName,
        context => RateLimitPartition.GetFixedWindowLimiter(
            GetRequestPartitionKey(context),
            _ => CreateFixedWindowOptions(rateLimitingOptions.Auth)));

    options.AddPolicy(
        AppConstants.ContentMutationRateLimitPolicyName,
        context => RateLimitPartition.GetFixedWindowLimiter(
            GetRequestPartitionKey(context),
            _ => CreateFixedWindowOptions(rateLimitingOptions.Mutation)));

    options.AddPolicy(
        AppConstants.GenerationRateLimitPolicyName,
        context => RateLimitPartition.GetFixedWindowLimiter(
            GetRequestPartitionKey(context),
            _ => CreateFixedWindowOptions(rateLimitingOptions.Generation)));
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppConstants.DefaultCorsPolicyName, policyBuilder =>
    {
        var allowedOrigins =
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? AppConstants.DefaultAllowedOrigins.ToArray();

        policyBuilder.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/notification"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPoiService, PoiService>();
builder.Services.AddScoped<ITranslationService, TranslationService>();
builder.Services.AddScoped<IGeofenceService, GeofenceService>();
builder.Services.AddScoped<IAudioService, AudioService>();
builder.Services.AddScoped<IAudioGenerationScheduler, AudioGenerationScheduler>();
builder.Services.AddScoped<IAudioGenerationProcessor, AudioGenerationProcessor>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddSingleton<QrPublicLinkBuilder>();
builder.Services.AddSingleton<IQrWebPresenceTracker, InMemoryQrWebPresenceTracker>();
builder.Services.AddSingleton<IVisitorMobilePresenceTracker, InMemoryVisitorMobilePresenceTracker>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IModerationService, ModerationService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IVisitEventService, VisitEventService>();
builder.Services.AddScoped<IVisitEventRetentionService, VisitEventRetentionService>();
builder.Services.AddScoped<ITourService, TourService>();
builder.Services.AddScoped<IManagedLanguageService, ManagedLanguageService>();
builder.Services.AddSingleton<INotificationBroadcaster, SignalRNotificationBroadcaster>();
builder.Services.AddSingleton<IAudioGenerationQueue, InMemoryAudioGenerationQueue>();
builder.Services.AddHostedService<AudioGenerationWorker>();
builder.Services.AddHostedService<VisitEventRetentionWorker>();

if (googleCloudOptions.IsConfigured)
{
    builder.Services.AddSingleton<IGoogleAccessTokenProvider, GoogleServiceAccountTokenProvider>();
    builder.Services.AddHttpClient<GoogleCloudTranslationService>();
    builder.Services.AddHttpClient<GoogleCloudTtsService>();
    builder.Services.AddScoped<IGoogleTranslationService>(sp => sp.GetRequiredService<GoogleCloudTranslationService>());
    builder.Services.AddScoped<IGoogleTtsService>(sp => sp.GetRequiredService<GoogleCloudTtsService>());
}
else
{
    builder.Services.AddSingleton<IGoogleTranslationService, MockGoogleTranslationService>();
    builder.Services.AddSingleton<IGoogleTtsService, MockGoogleTtsService>();
}

if (cloudflareR2Options.IsConfigured)
{
    builder.Services.AddSingleton<IAmazonS3>(_ =>
    {
        var credentials = new BasicAWSCredentials(cloudflareR2Options.AccessKeyId, cloudflareR2Options.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL = cloudflareR2Options.ServiceUrl,
            AuthenticationRegion = "auto",
            ForcePathStyle = true
        };

        return new AmazonS3Client(credentials, config);
    });
    builder.Services.AddSingleton<IR2ObjectClient, AwsR2ObjectClient>();
    builder.Services.AddSingleton<IStorageService, CloudflareR2StorageService>();
}
else
{
    builder.Services.AddSingleton<IStorageService>(_ =>
    {
        var storageRoot = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "audio");
        return new MockStorageService(storageRoot);
    });
}

var app = builder.Build();

app.UseForwardedHeaders();

if (cloudflareR2Options.IsConfigured && string.IsNullOrWhiteSpace(cloudflareR2Options.PublicBaseUrl))
{
    app.Logger.LogInformation("Cloudflare R2 is configured without a PublicBaseUrl. Audio assets will fall back to /api/audio/{{id}}/stream URLs.");
}

if (!string.IsNullOrWhiteSpace(publicQrOptions.BaseUrl))
{
    app.Logger.LogInformation("Public QR links are configured to use {PublicQrBaseUrl}", publicQrOptions.BaseUrl);
}

if (mobileAppLinksOptions.Android.Count > 0)
{
    app.Logger.LogInformation("Android app links are configured for {AndroidAppLinkCount} package targets.", mobileAppLinksOptions.Android.Count);
}

if (visitEventRetentionOptions.Enabled)
{
    app.Logger.LogInformation(
        "Visit event retention is enabled for {RetentionDays} days.",
        Math.Clamp(visitEventRetentionOptions.RawEventRetentionDays, 1, 365));
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseCors(AppConstants.DefaultCorsPolicyName);
app.UseAuthentication();
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<RequestDiagnosticsMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHealthChecks("/healthz");

await InitializeDatabaseAsync(app);

app.Run();

static async Task InitializeDatabaseAsync(WebApplication app)
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();

    try
    {
        await dbContext.Database.MigrateAsync();
        await seeder.SeedAsync();
    }
    catch (Exception ex) when (app.Environment.IsDevelopment())
    {
        app.Logger.LogWarning(ex, "Database initialization was skipped because PostgreSQL is not currently reachable.");
    }
}

static FixedWindowRateLimiterOptions CreateFixedWindowOptions(RateLimitPolicyOptions options)
{
    return new FixedWindowRateLimiterOptions
    {
        PermitLimit = options.PermitLimit,
        Window = TimeSpan.FromSeconds(options.WindowSeconds),
        QueueLimit = options.QueueLimit,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        AutoReplenishment = true
    };
}

static string GetRequestPartitionKey(HttpContext context)
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!string.IsNullOrWhiteSpace(userId))
    {
        return $"user:{userId}";
    }

    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return $"ip:{forwardedFor}";
    }

    var remoteIp = context.Connection.RemoteIpAddress?.ToString();
    return string.IsNullOrWhiteSpace(remoteIp) ? "anonymous" : $"ip:{remoteIp}";
}

static ProblemDetails CreateProblemDetails(HttpContext context, int statusCode, string title, string detail)
{
    var problemDetails = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail
    };

    problemDetails.Extensions["traceId"] = context.TraceIdentifier;

    var correlationId = GetCorrelationId(context);
    if (!string.IsNullOrWhiteSpace(correlationId))
    {
        problemDetails.Extensions["correlationId"] = correlationId;
    }

    return problemDetails;
}

static string? GetCorrelationId(HttpContext context)
{
    if (context.Response.Headers.TryGetValue(AppConstants.CorrelationIdHeaderName, out var responseHeader)
        && !string.IsNullOrWhiteSpace(responseHeader.ToString()))
    {
        return responseHeader.ToString();
    }

    if (context.Request.Headers.TryGetValue(AppConstants.CorrelationIdHeaderName, out var requestHeader)
        && !string.IsNullOrWhiteSpace(requestHeader.ToString()))
    {
        return requestHeader.ToString();
    }

    return context.TraceIdentifier;
}

public partial class Program;
