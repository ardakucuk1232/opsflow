using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.UnitTests.Pagination;

public class PageRequestTests
{
    [Fact]
    public void Defaults_AreFirstPageWithDefaultSize()
    {
        var request = new PageRequest();

        Assert.Equal(1, request.Page);
        Assert.Equal(PageRequest.DefaultPageSize, request.PageSize);
        Assert.Equal(0, request.Skip);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Page_BelowOne_IsTreatedAsFirstPage(int page)
    {
        var request = new PageRequest { Page = page };

        Assert.Equal(1, request.Page);
    }

    [Theory]
    [InlineData(0, PageRequest.DefaultPageSize)]
    [InlineData(-1, PageRequest.DefaultPageSize)]
    [InlineData(50, 50)]
    [InlineData(PageRequest.MaxPageSize + 1, PageRequest.MaxPageSize)]
    [InlineData(100_000, PageRequest.MaxPageSize)]
    public void PageSize_IsKeptWithinAllowedRange(int requested, int expected)
    {
        var request = new PageRequest { PageSize = requested };

        Assert.Equal(expected, request.PageSize);
    }

    [Fact]
    public void Skip_IsCalculatedFromPageAndPageSize()
    {
        var request = new PageRequest { Page = 3, PageSize = 25 };

        Assert.Equal(50, request.Skip);
    }
}
