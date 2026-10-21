using OpsFlow.Application.Common.Persistence;

namespace OpsFlow.UnitTests.Text;

public class SqlTextTests
{
    [Theory]
    [InlineData("rapor", "%rapor%")]
    [InlineData("100%", "%100\\%%")]
    [InlineData("snake_case", "%snake\\_case%")]
    [InlineData("a\\b", "%a\\\\b%")]
    public void ContainsPattern_EscapesLikeWildcards(string term, string expected)
    {
        Assert.Equal(expected, SqlText.ContainsPattern(term));
    }

    [Fact]
    public void Lower_CannotRunOutsideAQuery()
    {
        Assert.Throws<NotSupportedException>(() => SqlText.Lower("x"));
    }
}
