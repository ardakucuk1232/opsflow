using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Action).HasMaxLength(20);
        builder.Property(a => a.EntityName).HasMaxLength(100);
        builder.Property(a => a.EntityId).HasMaxLength(100);
        builder.Property(a => a.IpAddress).HasMaxLength(64);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        builder.Property(a => a.OldValues).HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnType("jsonb");

        builder.HasIndex(a => new { a.CompanyId, a.EntityName, a.EntityId });

        builder.HasIndex(a => new { a.CompanyId, a.CreatedAt });

        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => new { a.CompanyId, a.UserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Company)
               .WithMany()
               .HasForeignKey(a => a.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}