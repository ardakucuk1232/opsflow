using OpsFlow.Application.Common.Text;

namespace OpsFlow.UnitTests.Text;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("ABC Yazılım A.Ş.", "abc-yazilim-a-s")]
    [InlineData("Çağrı İletişim", "cagri-iletisim")]
    [InlineData("  Öz   Güven  Ltd ", "oz-guven-ltd")]
    [InlineData("Café & Co", "cafe-co")]
    [InlineData("XYZ Teknoloji 2026", "xyz-teknoloji-2026")]
    public void Generate_ConvertsCompanyNameToUrlFriendlySlug(string input, string expected)
    {
        var slug = SlugGenerator.Generate(input);

        Assert.Equal(expected, slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData("!!!")]
    [InlineData("   ")]
    public void Generate_ReturnsFallback_WhenInputHasNoUsableCharacters(string input)
    {
        var slug = SlugGenerator.Generate(input);

        Assert.Equal("company", slug);
    }

    [Fact]
    public void Generate_TruncatesLongInput_WithoutTrailingDash()
    {
        var input = new string('a', 49) + " bbbb";

        var slug = SlugGenerator.Generate(input);

        Assert.True(slug.Length <= 50);
        Assert.False(slug.EndsWith('-'));
        Assert.Equal(new string('a', 49), slug);
    }
}