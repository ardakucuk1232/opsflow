using OpsFlow.Infrastructure.Tenancy;

namespace OpsFlow.UnitTests.Tenancy;

public class TenantContextTests
{
    [Fact]
    public void NewContext_HasNoTenant()
    {
        var context = new TenantContext();

        Assert.False(context.HasTenant);
        Assert.Null(context.CompanyId);
        Assert.Null(context.UserId);
    }

    [Fact]
    public void Set_StoresCompanyAndUser()
    {
        var context = new TenantContext();
        var companyId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        context.Set(companyId, userId);

        Assert.True(context.HasTenant);
        Assert.Equal(companyId, context.CompanyId);
        Assert.Equal(userId, context.UserId);
    }

    [Fact]
    public void Set_ThrowsWhenCalledTwice()
    {
        var context = new TenantContext();
        context.Set(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.Throws<InvalidOperationException>(
            () => context.Set(Guid.CreateVersion7(), Guid.CreateVersion7()));
    }
}