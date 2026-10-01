using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.HasAlternateKey(t => new { t.CompanyId, t.Id });

        builder.Property(t => t.Title).HasMaxLength(300);
        builder.Property(t => t.Description).HasMaxLength(10000);

        builder.HasIndex(t => new { t.CompanyId, t.ProjectId, t.Number })
               .IsUnique();

        builder.HasIndex(t => new { t.CompanyId, t.ProjectId, t.Status, t.BoardOrder });

        builder.HasOne(t => t.Project)
               .WithMany(p => p.Tasks)
               .HasForeignKey(t => new { t.CompanyId, t.ProjectId })
               .HasPrincipalKey(p => new { p.CompanyId, p.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Assignee)
               .WithMany(u => u.AssignedTasks)
               .HasForeignKey(t => new { t.CompanyId, t.AssigneeId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Reporter)
               .WithMany()
               .HasForeignKey(t => new { t.CompanyId, t.ReporterId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Company)
               .WithMany()
               .HasForeignKey(t => t.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}