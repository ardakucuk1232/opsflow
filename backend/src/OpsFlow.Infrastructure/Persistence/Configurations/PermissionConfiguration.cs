using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence.Seed;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Code).HasMaxLength(100);
        builder.Property(p => p.Description).HasMaxLength(300);
        builder.Property(p => p.Group).HasMaxLength(50);

        builder.HasIndex(p => p.Code).IsUnique();
        
        builder.HasData(PermissionSeed.All);
    }
}