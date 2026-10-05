using Microsoft.EntityFrameworkCore;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Persistence;
using OpsFlow.Infrastructure.Persistence.Interceptors;
using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.UnitTests.Tenancy;

public class TenantGuardInterceptorTests
{
    private static readonly Guid TenantA = Guid.CreateVersion7();
    private static readonly Guid TenantB = Guid.CreateVersion7();

    [Fact]
    public void Added_WithoutCompanyId_IsAssignedToCurrentTenant()
    {
        var tenant = TenantOf(TenantA);
        using var db = CreateDb(tenant);
        var guard = new TenantGuardInterceptor(tenant);

        var token = NewToken(companyId: Guid.Empty);
        db.RefreshTokens.Add(token);

        guard.Enforce(db);

        Assert.Equal(TenantA, token.CompanyId);
    }

    [Fact]
    public void Added_ForAnotherTenant_Throws()
    {
        var tenant = TenantOf(TenantA);
        using var db = CreateDb(tenant);
        var guard = new TenantGuardInterceptor(tenant);

        db.RefreshTokens.Add(NewToken(companyId: TenantB));

        Assert.Throws<TenantIsolationException>(() => guard.Enforce(db));
    }

    [Fact]
    public void Added_WithoutTenantAndWithoutCompanyId_Throws()
    {
        var noTenant = new TenantContext();
        using var db = CreateDb(noTenant);
        var guard = new TenantGuardInterceptor(noTenant);

        db.RefreshTokens.Add(NewToken(companyId: Guid.Empty));

        Assert.Throws<TenantIsolationException>(() => guard.Enforce(db));
    }

    [Fact]
    public void Added_WithoutTenantButWithExplicitCompanyId_IsAllowed()
    {
        var noTenant = new TenantContext();
        using var db = CreateDb(noTenant);
        var guard = new TenantGuardInterceptor(noTenant);

        db.RefreshTokens.Add(NewToken(companyId: TenantA));

        var exception = Record.Exception(() => guard.Enforce(db));

        Assert.Null(exception);
    }

    [Fact]
    public void Modified_WhenCompanyIdChanges_Throws()
    {
        var tenant = TenantOf(TenantA);
        using var db = CreateDb(tenant);
        var guard = new TenantGuardInterceptor(tenant);

        var token = NewToken(companyId: TenantA);
        db.Attach(token);

        token.CompanyId = TenantB;

        Assert.Throws<TenantIsolationException>(() => guard.Enforce(db));
    }

    [Fact]
    public void Modified_EntityOfAnotherTenant_Throws()
    {
        var tenant = TenantOf(TenantA);
        using var db = CreateDb(tenant);
        var guard = new TenantGuardInterceptor(tenant);

        var token = NewToken(companyId: TenantB);
        db.Attach(token);

        token.TokenHash = "changed-hash";

        Assert.Throws<TenantIsolationException>(() => guard.Enforce(db));
    }

    [Fact]
    public void Deleted_EntityOfAnotherTenant_Throws()
    {
        var tenant = TenantOf(TenantA);
        using var db = CreateDb(tenant);
        var guard = new TenantGuardInterceptor(tenant);

        var token = NewToken(companyId: TenantB);
        db.Attach(token);

        db.Remove(token);

        Assert.Throws<TenantIsolationException>(() => guard.Enforce(db));
    }

    private static TenantContext TenantOf(Guid companyId)
    {
        var context = new TenantContext();
        context.Set(companyId, Guid.CreateVersion7());
        return context;
    }

    private static OpsFlowDbContext CreateDb(TenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<OpsFlowDbContext>()
            .UseNpgsql("Host=unused;Database=unused")
            .Options;

        return new OpsFlowDbContext(options, tenantContext);
    }

    private static RefreshToken NewToken(Guid companyId) => new()
    {
        CompanyId = companyId,
        UserId = Guid.CreateVersion7(),
        TokenHash = "test-hash",
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
    };
}