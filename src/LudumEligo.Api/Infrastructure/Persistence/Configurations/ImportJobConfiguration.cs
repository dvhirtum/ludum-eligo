using LudumEligo.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudumEligo.Api.Infrastructure.Persistence.Configurations;

internal sealed class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public const string ActiveImportIndex = "ix_import_jobs_active_user";

    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.Error).HasMaxLength(1000);
        builder.HasOne<User>().WithMany().HasForeignKey(j => j.UserId).OnDelete(DeleteBehavior.Cascade);

        // At most one queued or running import per user.
        builder.HasIndex(j => j.UserId)
            .IsUnique()
            .HasFilter("status IN ('Queued', 'Running')")
            .HasDatabaseName(ActiveImportIndex);

        // The latest import per user, and the worker's queue.
        builder.HasIndex(j => new { j.UserId, j.CreatedAt });
        builder.HasIndex(j => new { j.Status, j.CreatedAt });
    }
}
