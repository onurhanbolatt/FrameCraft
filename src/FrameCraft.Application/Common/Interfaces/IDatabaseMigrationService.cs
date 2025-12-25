namespace FrameCraft.Application.Common.Interfaces;

public interface IDatabaseMigrationService
{
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
}