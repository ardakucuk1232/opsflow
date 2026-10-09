namespace OpsFlow.Application.Features.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskSummaryDto>> ListForProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<AssignedTaskDto>> ListAssignedToMeAsync(CancellationToken cancellationToken);

    Task<TaskDetailDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<TaskDetailDto> CreateAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskDetailDto> UpdateAsync(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskDetailDto> AssignAsync(Guid id, AssignTaskRequest request, CancellationToken cancellationToken);

    Task<TaskDetailDto> MoveAsync(Guid id, MoveTaskRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
