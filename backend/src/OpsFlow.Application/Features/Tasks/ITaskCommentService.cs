namespace OpsFlow.Application.Features.Tasks;

public interface ITaskCommentService
{
    Task<IReadOnlyList<TaskCommentDto>> ListAsync(Guid taskId, CancellationToken cancellationToken);

    Task<TaskCommentDto> AddAsync(Guid taskId, AddTaskCommentRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken);
}
