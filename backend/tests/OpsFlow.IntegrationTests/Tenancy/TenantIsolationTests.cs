using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Tenancy;
using OpsFlow.IntegrationTests.Fixtures;

namespace OpsFlow.IntegrationTests.Tenancy;

public class TenantIsolationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _postgres;

    public TenantIsolationTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }


    [Fact]
    public async Task QueryFilter_ReturnsOnlyCurrentTenantsRecords()
    {
        var companyA = await _postgres.SeedCompanyAsync();
        await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: companyA.CompanyId);

        var users = await db.Users.ToListAsync();

        var user = Assert.Single(users);
        Assert.Equal(companyA.UserId, user.Id);
    }

    [Fact]
    public async Task QueryFilter_HidesAnotherTenantsRecord_EvenWhenItsIdIsKnown()
    {
        var companyA = await _postgres.SeedCompanyAsync();
        var companyB = await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: companyA.CompanyId);

        var foreignUser = await db.Users.SingleOrDefaultAsync(u => u.Id == companyB.UserId);

        Assert.Null(foreignUser);
    }

    [Fact]
    public async Task QueryFilter_ReturnsNothing_WhenThereIsNoTenant()
    {
        await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: null);

        var visibleUsers = await db.Users.ToListAsync();
        var allUsers = await db.Users.IgnoreQueryFilters().ToListAsync();

        Assert.Empty(visibleUsers);
        Assert.NotEmpty(allUsers);
    }


    [Fact]
    public async Task Guard_AssignsCurrentTenant_WhenCompanyIdIsNotSet()
    {
        var companyA = await _postgres.SeedCompanyAsync();

        var role = new Role { Name = "Custom role" };

        await using (var db = _postgres.CreateDbContext(tenantId: companyA.CompanyId))
        {
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        await using var verification = _postgres.CreateDbContext(tenantId: null);

        var saved = await verification.Roles
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Id == role.Id);

        Assert.Equal(companyA.CompanyId, saved.CompanyId);
    }

    [Fact]
    public async Task Guard_RejectsCreatingRecordForAnotherTenant()
    {
        var companyA = await _postgres.SeedCompanyAsync();
        var companyB = await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: companyA.CompanyId);

        db.Roles.Add(new Role { CompanyId = companyB.CompanyId, Name = "Intruder" });

        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Guard_RejectsModifyingAnotherTenantsRecord_LoadedWithIgnoreQueryFilters()
    {
        var companyA = await _postgres.SeedCompanyAsync();
        var companyB = await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: companyA.CompanyId);

        var foreignUser = await db.Users
            .IgnoreQueryFilters()
            .SingleAsync(u => u.Id == companyB.UserId);

        foreignUser.FirstName = "Changed";

        await Assert.ThrowsAsync<TenantIsolationException>(() => db.SaveChangesAsync());
    }


    [Fact]
    public async Task Database_RejectsCrossTenantRelationship_WhenApplicationLayersAreBypassed()
    {
        var companyA = await _postgres.SeedCompanyAsync();
        var companyB = await _postgres.SeedCompanyAsync();

        await using var db = _postgres.CreateDbContext(tenantId: null);

        db.UserRoles.Add(new UserRole
        {
            CompanyId = companyA.CompanyId,
            UserId = companyA.UserId,
            RoleId = companyB.RoleId,
            AssignedAt = DateTimeOffset.UtcNow
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, postgresException.SqlState);
    }
}