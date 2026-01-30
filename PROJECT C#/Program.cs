using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Services;
using PROJECT_C_.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Identity
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.WithOrigins("https://localhost:7090", "http://localhost:7090", "https://localhost:5002", "http://localhost:5002")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IDistanceCalculator, DistanceCalculator>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Backend", Version = "v1" });
    options.CustomSchemaIds(type => (type.FullName ?? type.Name).Replace("+", ".")); // Safer schema IDs
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First()); // Nuclear fix for conflicts
    
    // Add Security Definition for Bearer
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Please enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
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
            new string[] {}
        }
    });
});

// ? MCP server (HTTP transport)
    // .AddMcpServer()
    // .WithHttpTransport();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// TEMPORARILY DISABLED FOR SWAGGER DEBUG
// app.MapIdentityApi<IdentityUser>();

app.MapControllers();

// ? MCP endpoint
    // app.MapMcp();

app.Run();
