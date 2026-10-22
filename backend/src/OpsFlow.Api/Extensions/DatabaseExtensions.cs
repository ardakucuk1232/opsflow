using Microsoft.EntityFrameworkCore;
using OpsFlow.Infrastructure.Persistence;

namespace OpsFlow.Api.Extensions;

public static class DatabaseExtensions
{
    public const string MigrateArgument = "--migrate";

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpsFlowDbContext>>();

        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();

        logger.LogInformation("Applying {Count} pending migration(s).", pending.Count);

        await db.Database.MigrateAsync();

        logger.LogInformation("The database is up to date.");
    }
}
