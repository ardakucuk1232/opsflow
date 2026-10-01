using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.HasAlternateKey(r => new { r.CompanyId, r.Id});

        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.Description).HasMaxLength(500);

        builder.HasIndex(r => new { r.CompanyId, r.Name }).IsUnique();

        builder.HasOne(r => r.Company).WithMany(c => c.Roles).HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}