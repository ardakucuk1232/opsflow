using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Persistence;
using OpsFlow.Domain.Common;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Persistence;

public class OpsFlowDbContext : DbContext, IOpsFlowDbContext
{
    private static readonly MethodInfo ApplyTenantFilterMethod = typeof(OpsFlowDbContext)
        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ApplySoftDeleteFilterMethod = typeof(OpsFlowDbContext)
        .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly ITenantContext _tenantContext;

    public OpsFlowDbContext(
        DbContextOptions<OpsFlowDbContext> options,
        ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    private Guid? CurrentCompanyId => _tenantContext.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        ApplyGlobalFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private void ApplyGlobalFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>()
            .HasQueryFilter(QueryFilterNames.Tenant, company => company.Id == CurrentCompanyId);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            if (typeof(ITenantEntity).IsAssignableFrom(clrType))
            {
                ApplyTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                ApplySoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [modelBuilder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(QueryFilterNames.Tenant, entity => entity.CompanyId == CurrentCompanyId);
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(QueryFilterNames.SoftDelete, entity => !entity.IsDeleted);
    }
}