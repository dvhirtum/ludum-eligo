using LudumEligo.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LudumEligo.Api.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.GoogleSubject).HasMaxLength(255);
        builder.HasIndex(u => u.GoogleSubject).IsUnique();
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(320);
    }
}
