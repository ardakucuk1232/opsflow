using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Body).HasMaxLength(10000);

        builder.HasOne(c => c.TaskItem)
               .WithMany(t => t.Comments)
               .HasForeignKey(c => new { c.CompanyId, c.TaskItemId })
               .HasPrincipalKey(t => new { t.CompanyId, t.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Author)
               .WithMany()
               .HasForeignKey(c => new { c.CompanyId, c.AuthorId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Company)
               .WithMany()
               .HasForeignKey(c => c.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}