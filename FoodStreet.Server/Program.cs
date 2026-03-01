using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Services;
using PROJECT_C_.Services.Interfaces;
using PROJECT_C_.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURATION
// ========================================
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured in appsettings.json");

// ========================================
// CONTROLLERS & JSON
// ========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// ========================================
// IDENTITY (User Management)
// ========================================
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // User settings
    options.User.RequireUniqueEmail = true;

    // Dùng tên ngắn JWT cho Identity (khớp với token)
    options.ClaimsIdentity.UserIdClaimType = "sub";
    options.ClaimsIdentity.UserNameClaimType = "name";
    options.ClaimsIdentity.EmailClaimType = "email";
    options.ClaimsIdentity.RoleClaimType = "role";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ========================================
// JWT AUTHENTICATION
// ========================================
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.MapInboundClaims = false; // Giữ nguyên tên ngắn từ JWT
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "role",
        NameClaimType = "name"
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Claims Transformation: đọc JWT trực tiếp từ header, thêm role claims
builder.Services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, 
    FoodStreet.Server.Services.JwtClaimsTransformation>();

// ========================================
// CORS
// ========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7090", 
                "http://localhost:7090", 
                "https://localhost:5002", 
                "http://localhost:5002")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ========================================
// DATABASE
// ========================================
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// ========================================
// APPLICATION SERVICES
// ========================================
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IDistanceCalculator, DistanceCalculator>();

// ========================================
// SWAGGER / OPENAPI
// ========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo 
    { 
        Title = "FoodStreet API", 
        Version = "v1",
        Description = "Multilingual Food Street Guide API with JWT Authentication"
    });
    
    options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", "."));
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    
    // JWT Bearer Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by space and your JWT token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ========================================
// BUILD & CONFIGURE MIDDLEWARE
// ========================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseStaticFiles();

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed sample data
await SeedData.InitializeAsync(app.Services);

app.Run();
