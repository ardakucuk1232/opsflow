using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.HasAlternateKey(p => new { p.CompanyId, p.Id });

        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Key).HasMaxLength(10);
        builder.Property(p => p.Description).HasMaxLength(4000);

        builder.HasIndex(p => new { p.CompanyId, p.Key })
               .IsUnique()
               .HasFilter("\"IsDeleted\" = false");

        builder.HasOne(p => p.CreatedByUser)
               .WithMany()
               .HasForeignKey(p => new { p.CompanyId, p.CreatedByUserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Company)
               .WithMany(c => c.Projects)
               .HasForeignKey(p => p.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}