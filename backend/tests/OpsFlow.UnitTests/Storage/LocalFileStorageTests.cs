using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Options;
using OpsFlow.Infrastructure.Storage;

namespace OpsFlow.UnitTests.Storage;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), "opsflow-unit", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SaveOpenAndDelete_WorkUnderARelativeRoot()
    {
        var storage = CreateStorage("uploads");

        await storage.SaveAsync("company/file", new MemoryStream([1, 2, 3]), CancellationToken.None);

        Assert.True(File.Exists(Path.Combine(_contentRoot, "uploads", "company", "file")));

        await using (var stream = storage.OpenRead("company/file"))
        {
            var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            Assert.Equal([1, 2, 3], buffer.ToArray());
        }

        storage.Delete("company/file");
        storage.Delete("company/file");

        Assert.False(File.Exists(Path.Combine(_contentRoot, "uploads", "company", "file")));
    }

    [Fact]
    public async Task Save_DoesNotOverwriteAnExistingFile()
    {
        var storage = CreateStorage("uploads");

        await storage.SaveAsync("file", new MemoryStream([1]), CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() => storage.SaveAsync("file", new MemoryStream([2]), CancellationToken.None));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("company/../../outside")]
    [InlineData("/etc/passwd")]
    public async Task PathsOutsideTheRoot_AreRejected(string path)
    {
        var storage = CreateStorage("uploads");

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.SaveAsync(path, new MemoryStream([1]), CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() => storage.OpenRead(path));
        Assert.Throws<InvalidOperationException>(() => storage.Delete(path));
    }

    private LocalFileStorage CreateStorage(string rootPath) =>
        new(Options.Create(new StorageOptions { RootPath = rootPath }), new TestEnvironment(_contentRoot));

    private sealed class TestEnvironment(string contentRoot) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = "OpsFlow.UnitTests";

        public string ContentRootPath { get; set; } = contentRoot;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
