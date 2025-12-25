using FrameCraft.API.HealthChecks;
using FrameCraft.API.Middleware;
using FrameCraft.Application;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Infrastructure;
using FrameCraft.Infrastructure.HealthChecks;
using FrameCraft.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

// Bootstrap logger
// Uygulama ayağa kalkmadan ÖNCE log alabilmek için minimal Serilog konfigürasyonu
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    // Uygulama başlangıç logu
    Log.Information("FrameCraft API starting up...");

    // WebApplication builder oluşturulur
    var builder = WebApplication.CreateBuilder(args);

    // Serilog ana konfigürasyonu
    // appsettings.json + DI servisleri + Environment bilgisi ile zenginleştirilir
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName));

    // Application katmanı DI kayıtları
    builder.Services.AddApplication();

    // HttpContext'e servislerden erişebilmek için
    builder.Services.AddHttpContextAccessor();

    // Infrastructure katmanı (DB, S3, repository vb.)
    builder.Services.AddInfrastructure(builder.Configuration);

    // === JWT AUTHENTICATION ===

    // JwtSettings bölümünü config'ten alır
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["Secret"];

    // Authentication middleware ayarları
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // Token doğrulama kuralları
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,              // Issuer kontrolü
            ValidateAudience = true,            // Audience kontrolü
            ValidateLifetime = true,            // Token süresi kontrolü
            ValidateIssuerSigningKey = true,    // İmza anahtarı kontrolü
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
            ClockSkew = TimeSpan.Zero            // Süre toleransı kapatılır
        };
    });

    // === AUTHORIZATION POLICIES ===
    builder.Services.AddAuthorization(options =>
    {
        // Sadece IsSuperAdmin claim'i true olanlar
        options.AddPolicy("SuperAdminOnly", policy =>
            policy.RequireAssertion(ctx =>
                string.Equals(
                    ctx.User.FindFirst("IsSuperAdmin")?.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase)));

        // Admin veya SuperAdmin erişimi
        options.AddPolicy("AdminOnly", policy =>
            policy.RequireAssertion(ctx =>
            {
                var isSuper = string.Equals(
                    ctx.User.FindFirst("IsSuperAdmin")?.Value,
                    "true",
                    StringComparison.OrdinalIgnoreCase);

                var isAdminRole = ctx.User.IsInRole("Admin");

                return isSuper || isAdminRole;
            }));

        // Tenant bazlı kullanıcı kontrolü
        options.AddPolicy("TenantUser", policy =>
            policy.RequireClaim("TenantId"));
    });

    // Controller desteği eklenir
    builder.Services.AddControllers();

    // Minimal API / Swagger için endpoint keşfi
    builder.Services.AddEndpointsApiExplorer();

    // === SWAGGER CONFIG ===
    builder.Services.AddSwaggerGen(options =>
    {
        // JWT Bearer tanımı
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "JWT Authorization header. Example: \"Bearer {token}\""
        });

        // Swagger'da authorize butonunun aktif olması için
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

    // === CORS ===
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:3001"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // === HEALTH CHECKS ===
    builder.Services.AddHealthChecks()
        // Veritabanı health check
        .AddDbContextCheck<ApplicationDbContext>(
            name: "database",
            tags: new[] { "db", "sql", "ready" })
        // S3 erişim health check
        .AddCheck<S3HealthCheck>(
            name: "s3-storage",
            tags: new[] { "s3", "storage", "ready" });

    // Uygulama build edilir
    var app = builder.Build();

    // === DATABASE MIGRATION ===
    // Uygulama ayağa kalkarken otomatik migration
    using (var scope = app.Services.CreateScope())
    {
        var migrationService = scope.ServiceProvider.GetRequiredService<IDatabaseMigrationService>();
        await migrationService.ApplyMigrationsAsync();
    }

    // === MIDDLEWARE PIPELINE ===
    // Her request için CorrelationId üretir
    app.UseCorrelationId();

    // Global exception handling
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Request/Response loglama
    app.UseRequestLogging();

    // Swagger sadece development ortamında açılır
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTPS yönlendirmesi
    app.UseHttpsRedirection();

    // CORS middleware
    app.UseCors("AllowFrontend");

    // Authentication & Authorization
    app.UseAuthentication();
    app.UseAuthorization();

    // Tenant belirleme middleware'i
    app.UseTenantMiddleware();

    // === HEALTH ENDPOINTS ===

    // Genel health endpoint
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });

    // Ready check (DB + S3)
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = HealthCheckResponseWriter.WriteResponse
    });

    // Liveness check (sadece uygulama ayakta mı)
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Controller route'ları
    app.MapControllers();

    // Başarılı başlangıç logu
    Log.Information(
        "FrameCraft API started successfully. Environment: {Environment}",
        app.Environment.EnvironmentName);

    app.Run();
}
catch (Exception ex)
{
    // Startup sırasında fatal hata
    Log.Fatal(ex, "FrameCraft API failed to start");
    throw;
}
finally
{
    // Uygulama kapanış logu
    Log.Information("FrameCraft API shutting down...");
    Log.CloseAndFlush();
}