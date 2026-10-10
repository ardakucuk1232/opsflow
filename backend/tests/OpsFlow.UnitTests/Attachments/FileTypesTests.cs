using System.Text;
using OpsFlow.Application.Features.Attachments;

namespace OpsFlow.UnitTests.Attachments;

public class FileTypesTests
{
    [Theory]
    [InlineData("report.pdf", "application/pdf")]
    [InlineData("PHOTO.JPG", "image/jpeg")]
    [InlineData("data.csv", "text/csv")]
    [InlineData("plan.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void TryGetContentType_MapsAllowedExtensions(string fileName, string expected)
    {
        Assert.True(FileTypes.TryGetContentType(fileName, out var contentType));
        Assert.Equal(expected, contentType);
    }

    [Theory]
    [InlineData("script.js")]
    [InlineData("index.html")]
    [InlineData("drawing.svg")]
    [InlineData("tool.exe")]
    [InlineData("archive.pdf.exe")]
    [InlineData("README")]
    public void TryGetContentType_RejectsEverythingElse(string fileName)
    {
        Assert.False(FileTypes.TryGetContentType(fileName, out _));
    }

    [Fact]
    public void HeaderMatches_ChecksTheSignatureOfBinaryFiles()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
        var pdf = "%PDF-1.7"u8.ToArray();

        Assert.True(FileTypes.HeaderMatches("image.png", png));
        Assert.True(FileTypes.HeaderMatches("doc.pdf", pdf));
        Assert.False(FileTypes.HeaderMatches("image.png", pdf));
        Assert.False(FileTypes.HeaderMatches("doc.pdf", png));
        Assert.False(FileTypes.HeaderMatches("image.png", []));
    }

    [Fact]
    public void HeaderMatches_RecognisesWebpOnlyWithItsRiffSubtype()
    {
        var webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
        var wav = "RIFF\0\0\0\0WAVEfmt "u8.ToArray();

        Assert.True(FileTypes.HeaderMatches("image.webp", webp));
        Assert.False(FileTypes.HeaderMatches("image.webp", wav));
    }

    [Fact]
    public void HeaderMatches_RejectsBinaryContentInTextFiles()
    {
        Assert.True(FileTypes.HeaderMatches("notes.txt", Encoding.UTF8.GetBytes("Toplantı notları")));
        Assert.False(FileTypes.HeaderMatches("notes.txt", [0x4D, 0x5A, 0x90, 0x00]));
    }
}
