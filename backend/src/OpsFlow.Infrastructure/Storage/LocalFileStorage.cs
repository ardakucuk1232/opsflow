using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Options;

namespace OpsFlow.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.RootPath;

        _root = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured));
    }

    public async Task SaveAsync(string path, Stream content, CancellationToken cancellationToken)
    {
        var fullPath = Resolve(path);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
    }

    public Stream OpenRead(string path) =>
        new FileStream(Resolve(path), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);

    public void Delete(string path)
    {
        var fullPath = Resolve(path);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }

    private string Resolve(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, path));

        if (!fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The storage path points outside of the storage root.");
        }

        return fullPath;
    }
}
