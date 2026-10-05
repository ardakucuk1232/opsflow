using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence;
using OpsFlow.Infrastructure.Persistence.Interceptors;
using OpsFlow.Infrastructure.Tenancy;
using Testcontainers.PostgreSql;

namespace OpsFlow.IntegrationTests.Fixtures;

public sealed record SeededCompany(Guid CompanyId, Guid UserId, Guid RoleId);

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var db = CreateDbContext(tenantId: null);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public OpsFlowDbContext CreateDbContext(Guid? tenantId)
    {
        var tenantContext = new TenantContext();

        if (tenantId is Guid id)
        {
            tenantContext.Set(id, Guid.CreateVersion7());
        }

        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .AddInterceptors(
                new TenantGuardInterceptor(tenantContext),
                new AuditableEntityInterceptor(TimeProvider.System)).Options;

        return new OpsFlowDbContext(options, tenantContext);
    }

    public async Task<SeededCompany> SeedCompanyAsync()
    {
        var unique = Guid.NewGuid().ToString("N");

        var company = new Company
        {
            Name = $"Company {unique}",
            Slug = $"company-{unique}"
        };

        var role = new Role
        {
            CompanyId = company.Id,
            Name = "Admin",
            IsSystemRole = true
        };

        var user = new User
        {
            CompanyId = company.Id,
            Email = $"user-{unique}@test.local",
            PasswordHash = "not-a-real-hash",
            FirstName = "Test",
            LastName = "User"
        };

        user.UserRoles.Add(new UserRole
        {
            CompanyId = company.Id,
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });

        await using var db = CreateDbContext(tenantId: null);

        db.Companies.Add(company);
        db.Roles.Add(role);
        db.Users.Add(user);

        await db.SaveChangesAsync();

        return new SeededCompany(company.Id, user.Id, role.Id);
    }
}