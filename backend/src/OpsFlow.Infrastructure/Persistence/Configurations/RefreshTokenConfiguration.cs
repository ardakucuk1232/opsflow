using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash).HasMaxLength(128);

        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Ignore(t => t.IsActive);

        builder.HasOne(t => t.User)
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(t => new { t.CompanyId, t.UserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Company)
               .WithMany()
               .HasForeignKey(t => t.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}