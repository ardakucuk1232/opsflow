using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.FileName).HasMaxLength(255);
        builder.Property(a => a.StoredFileName).HasMaxLength(100);
        builder.Property(a => a.StoragePath).HasMaxLength(500);
        builder.Property(a => a.ContentType).HasMaxLength(100);

        builder.HasOne(a => a.TaskItem)
               .WithMany(t => t.Attachments)
               .HasForeignKey(a => new { a.CompanyId, a.TaskItemId })
               .HasPrincipalKey(t => new { t.CompanyId, t.Id })
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.UploadedByUser)
               .WithMany()
               .HasForeignKey(a => new { a.CompanyId, a.UploadedByUserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Company)
               .WithMany()
               .HasForeignKey(a => a.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}