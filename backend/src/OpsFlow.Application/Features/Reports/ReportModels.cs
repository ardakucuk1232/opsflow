using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Reports;

public sealed class ReportQuery
{
    public Guid? ProjectId { get; init; }

    public DateOnly? Today { get; init; }
}

public sealed record ReportTotalsDto(int ActiveProjects, int OpenTasks, int OverdueTasks, int UnassignedOpenTasks, int CompletedLast30Days);

public sealed record StatusCountDto(TaskItemStatus Status, int Count);

public sealed record PriorityCountDto(TaskPriority Priority, int Count);

public sealed record WeeklyActivityDto(DateOnly WeekStart, int Created, int Completed);

public sealed record WorkloadDto(UserReferenceDto User, int Open, int Overdue);

public sealed record ProjectProgressDto(Guid Id, string Key, string Name, ProjectStatus Status, int Total, int Done, int Open, int Overdue);

public sealed record ReportOverviewDto(
    DateOnly Today,
    ReportTotalsDto Totals,
    IReadOnlyList<StatusCountDto> ByStatus,
    IReadOnlyList<PriorityCountDto> OpenByPriority,
    IReadOnlyList<WeeklyActivityDto> Weekly,
    IReadOnlyList<WorkloadDto> Workload,
    IReadOnlyList<ProjectProgressDto> Projects);

public sealed record MySummaryDto(int Open, int Overdue, int DueThisWeek, int CompletedLast7Days);
