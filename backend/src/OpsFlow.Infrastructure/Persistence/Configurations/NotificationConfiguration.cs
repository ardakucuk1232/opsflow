using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.Title).HasMaxLength(200);
        builder.Property(n => n.Message).HasMaxLength(1000);
        builder.Property(n => n.RelatedEntityType).HasMaxLength(50);

        builder.HasIndex(n => new { n.CompanyId, n.RecipientUserId, n.IsRead });

        builder.HasOne(n => n.RecipientUser)
               .WithMany()
               .HasForeignKey(n => new { n.CompanyId, n.RecipientUserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Company)
               .WithMany()
               .HasForeignKey(n => n.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}