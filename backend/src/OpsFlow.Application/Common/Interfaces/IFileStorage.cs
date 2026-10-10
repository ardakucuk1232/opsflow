namespace OpsFlow.Application.Common.Interfaces;

public interface IFileStorage
{
    Task SaveAsync(string path, Stream content, CancellationToken cancellationToken);

    Stream OpenRead(string path);

    void Delete(string path);
}
