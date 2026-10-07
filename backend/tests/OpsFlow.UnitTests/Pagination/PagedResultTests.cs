using OpsFlow.Application.Common.Pagination;

namespace OpsFlow.UnitTests.Pagination;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 20, 0)]
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(100, 25, 4)]
    public void TotalPages_IsCalculatedFromTotalCountAndPageSize(int totalCount, int pageSize, int expectedPages)
    {
        var result = new PagedResult<int>([], Page: 1, PageSize: pageSize, TotalCount: totalCount);

        Assert.Equal(expectedPages, result.TotalPages);
    }

    [Fact]
    public void FirstPage_HasNoPreviousPage()
    {
        var result = new PagedResult<int>([1, 2], Page: 1, PageSize: 2, TotalCount: 5);

        Assert.False(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }

    [Fact]
    public void LastPage_HasNoNextPage()
    {
        var result = new PagedResult<int>([5], Page: 3, PageSize: 2, TotalCount: 5);

        Assert.True(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }

    [Fact]
    public void EmptyResult_HasNoPagesInEitherDirection()
    {
        var result = new PagedResult<int>([], Page: 1, PageSize: 20, TotalCount: 0);

        Assert.False(result.HasPreviousPage);
        Assert.False(result.HasNextPage);
    }
}
