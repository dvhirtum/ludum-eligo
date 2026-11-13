using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace LudumEligo.Api.Infrastructure.Persistence;

public static class PersistenceExtensions
{
    public const string ConnectionName = "ludumeligo";

    public static IHostApplicationBuilder AddPersistence(this IHostApplicationBuilder builder)
    {
        builder.AddNpgsqlDbContext<AppDbContext>(
            ConnectionName,
            configureDbContextOptions: options => options.UseSnakeCaseNamingConvention());
        return builder;
    }

    /// <summary>Applies pending migrations when <c>Database:MigrateOnStartup</c> is set (development and tests).</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Aspire creates the database before the API starts. Without this, EF queries the missing
        // history table on the first migration and logs a (harmless) error before creating it.
        if (await db.Database.CanConnectAsync())
        {
            await db.GetService<IHistoryRepository>().CreateIfNotExistsAsync();
        }

        await db.Database.MigrateAsync();
    }
}
