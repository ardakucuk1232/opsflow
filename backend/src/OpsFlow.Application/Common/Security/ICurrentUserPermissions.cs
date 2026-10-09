namespace OpsFlow.Application.Common.Security;

public interface ICurrentUserPermissions
{
    Task<IReadOnlySet<string>> GetAsync(CancellationToken cancellationToken);

    Task<bool> HasAsync(string permission, CancellationToken cancellationToken);
}
