using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using Minio.DataModel.Args;
using ShopfloorManager.Application.Common.Interfaces;
using ShopfloorManager.Infrastructure.Data;
using ShopfloorManager.Infrastructure.Services;

namespace ShopfloorManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' not found");

        services.AddDbContext<ShopfloorDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IShopfloorDbContext>(sp => sp.GetRequiredService<ShopfloorDbContext>());
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IMinioService, MinioService>();
        services.AddScoped<ISpcService, SpcService>();

        // MinIO client
        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(config["Minio:Endpoint"] ?? "localhost:9000")
                .WithCredentials(
                    config["Minio:AccessKey"] ?? "minioadmin",
                    config["Minio:SecretKey"] ?? "minioadmin123")
                .WithSSL(bool.TryParse(config["Minio:UseSsl"], out var ssl) && ssl)
                .Build());

        return services;
    }
}
