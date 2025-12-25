using Amazon;
using Amazon.S3;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Common.Models;
using FrameCraft.Application.Common.Settings;
using FrameCraft.Domain.Repositories.Authentication;
using FrameCraft.Domain.Repositories.Common;
using FrameCraft.Domain.Repositories.Core;
using FrameCraft.Domain.Repositories.CRM;
using FrameCraft.Infrastructure.HealthChecks;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Authentication;
using FrameCraft.Infrastructure.Repositories.Common;
using FrameCraft.Infrastructure.Repositories.Core;
using FrameCraft.Infrastructure.Repositories.CRM;
using FrameCraft.Infrastructure.Services.Database;
using FrameCraft.Infrastructure.Services.Identity;
using FrameCraft.Infrastructure.Services.MultiTenancy;
using FrameCraft.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FrameCraft.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

            options.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        // S3 Settings & Client
        var s3Settings = configuration.GetSection(S3Settings.SectionName).Get<S3Settings>();
        services.Configure<S3Settings>(configuration.GetSection(S3Settings.SectionName));

        if (s3Settings != null)
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(s3Settings.Region)
                };

                if (s3Settings.UseLocalStack)
                {
                    config.ServiceURL = s3Settings.LocalStackEndpoint;
                    config.ForcePathStyle = true;
                    config.UseHttp = true;
                }

                return new AmazonS3Client(
                    s3Settings.AccessKey,
                    s3Settings.SecretKey,
                    config);
            });

            // S3 Health Check
            services.AddSingleton<S3HealthCheck>();
        }

        // Tenant Context
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantProvider, TenantProvider>();

        // Current User Service
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // File Storage Service
        services.AddScoped<IFileStorageService, S3FileStorageService>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        return services;
    }
}