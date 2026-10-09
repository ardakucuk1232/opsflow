namespace OpsFlow.Application.Common.Interfaces;

public interface ITaskNumberGenerator
{
    Task<int> NextAsync(Guid projectId, CancellationToken cancellationToken);
}
