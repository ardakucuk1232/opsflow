using OpsFlow.Application.Common.Pagination;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Tasks;

public enum TaskSort
{
    Newest = 1,
    Oldest = 2,
    DueDate = 3,
    Priority = 4,
    Updated = 5
}

public sealed class TaskListQuery : PageRequest
{
    public string? Search { get; init; }

    public Guid? ProjectId { get; init; }

    public List<TaskItemStatus> Status { get; init; } = [];

    public List<TaskPriority> Priority { get; init; } = [];

    public Guid? AssigneeId { get; init; }

    public bool Unassigned { get; init; }

    public DateOnly? DueFrom { get; init; }

    public DateOnly? DueTo { get; init; }

    public TaskSort Sort { get; init; } = TaskSort.Newest;
}
