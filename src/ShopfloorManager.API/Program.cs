using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using ShopfloorManager.API.Hubs;
using ShopfloorManager.API.Infrastructure;
using ShopfloorManager.API.Middleware;
using ShopfloorManager.API.Services;
using ShopfloorManager.Application;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Infrastructure;
using ShopfloorManager.Infrastructure.Data;
using ShopfloorManager.Infrastructure.Mqtt;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shopfloor Manager API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            []
        }
    });
});

// JWT authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret chưa được cấu hình.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    });
builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IRealtimeNotifier, SignalRNotifier>();

// MQTT Background Service
builder.Services.Configure<MqttOptions>(o => {
    o.Broker = builder.Configuration["Mqtt:Broker"] ?? "localhost";
    o.Port   = int.TryParse(builder.Configuration["Mqtt:Port"], out var p) ? p : 1883;
    o.Topic  = builder.Configuration["Mqtt:TopicCnc"] ?? "factory/cnc/#";
});
builder.Services.AddHostedService<MqttBackgroundService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

QuestPDF.Settings.License = LicenseType.Community;

// ── Pipeline ──────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShopfloorDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAdminAsync(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ShopfloorHub>("/hub/shopfloor");
app.MapHub<MachineStatusHub>("/hub/machine-status");

app.Run();
