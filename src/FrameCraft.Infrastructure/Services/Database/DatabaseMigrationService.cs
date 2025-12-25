using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Infrastructure.Services.Database;

public class DatabaseMigrationService : IDatabaseMigrationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(
        ApplicationDbContext context,
        ILogger<DatabaseMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ApplyMigrationsAsync(CancellationToken cancellationToken = default)
    {
        var maxRetries = 5;
        var delay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _logger.LogInformation(
                    "Attempting database connection (attempt {Attempt}/{MaxRetries})...",
                    i + 1, maxRetries);

                if (await _context.Database.CanConnectAsync(cancellationToken))
                {
                    _logger.LogInformation("Database connection successful");

                    var pendingMigrations = await _context.Database
                        .GetPendingMigrationsAsync(cancellationToken);

                    if (pendingMigrations.Any())
                    {
                        _logger.LogInformation(
                            "Applying {Count} pending migrations...",
                            pendingMigrations.Count());

                        await _context.Database.MigrateAsync(cancellationToken);

                        _logger.LogInformation("Migrations applied successfully");
                    }
                    else
                    {
                        _logger.LogInformation("No pending migrations");
                    }

                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Database connection failed (attempt {Attempt}/{MaxRetries})",
                    i + 1, maxRetries);

                if (i == maxRetries - 1)
                {
                    _logger.LogError(ex, "An error occurred while migrating the database");
                    throw;
                }

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}