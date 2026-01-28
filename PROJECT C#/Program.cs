using Microsoft.EntityFrameworkCore;
using PROJECT_C_.Data;
using PROJECT_C_.Services;
using PROJECT_C_.Services.Interfaces;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IDistanceCalculator, DistanceCalculator>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ? MCP server (HTTP transport)
builder.Services
    .AddMcpServer()
    .WithHttpTransport();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// ? MCP endpoint
app.MapMcp();

app.Run();
