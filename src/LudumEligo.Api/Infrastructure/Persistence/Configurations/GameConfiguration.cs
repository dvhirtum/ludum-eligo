using LudumEligo.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudumEligo.Api.Infrastructure.Persistence.Configurations;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.HasKey(g => g.BggId);
        builder.Property(g => g.BggId).ValueGeneratedNever();
        builder.Property(g => g.Name).HasMaxLength(500);
        builder.Property(g => g.Weight).HasPrecision(5, 4);
        builder.Property(g => g.Mechanics).HasColumnType("text[]");
        builder.Property(g => g.Categories).HasColumnType("text[]");
    }
}
