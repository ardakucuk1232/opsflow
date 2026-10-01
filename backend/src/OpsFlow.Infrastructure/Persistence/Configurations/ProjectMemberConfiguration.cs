using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.HasKey(pm => new { pm.CompanyId, pm.ProjectId, pm.UserId });

        builder.HasOne(pm => pm.Project)
               .WithMany(p => p.Members)
               .HasForeignKey(pm => new { pm.CompanyId, pm.ProjectId })
               .HasPrincipalKey(p => new { p.CompanyId, p.Id })
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.User)
               .WithMany(u => u.ProjectMemberships)
               .HasForeignKey(pm => new { pm.CompanyId, pm.UserId })
               .HasPrincipalKey(u => new { u.CompanyId, u.Id })
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Company)
               .WithMany()
               .HasForeignKey(pm => pm.CompanyId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}