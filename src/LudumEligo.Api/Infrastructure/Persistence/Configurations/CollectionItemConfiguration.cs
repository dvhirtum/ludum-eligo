using LudumEligo.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudumEligo.Api.Infrastructure.Persistence.Configurations;

internal sealed class CollectionItemConfiguration : IEntityTypeConfiguration<CollectionItem>
{
    public void Configure(EntityTypeBuilder<CollectionItem> builder)
    {
        // The key is the unique (user_id, bgg_id) pair: each user has a game at most once.
        builder.HasKey(i => new { i.UserId, i.BggId });
        builder.Property(i => i.Source).HasConversion<string>().HasMaxLength(20);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Game).WithMany().HasForeignKey(i => i.BggId).OnDelete(DeleteBehavior.Restrict);
    }
}
