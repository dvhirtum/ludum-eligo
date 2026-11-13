using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LudumEligo.Api.Infrastructure.Persistence;

/// <summary>Lets <c>dotnet ef</c> create migrations without a running database.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=ludumeligo")
            .UseSnakeCaseNamingConvention()
            .Options);
}
