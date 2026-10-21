using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Reports;

public sealed class ReportService : IReportService
{
    public const int WeekCount = 8;
    private const int WorkloadLimit = 10;
    private const int ProjectLimit = 20;

    private static readonly TaskItemStatus[] OpenStatuses =
        [TaskItemStatus.Backlog, TaskItemStatus.Todo, TaskItemStatus.InProgress, TaskItemStatus.InReview];

    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;
    private readonly TimeProvider _timeProvider;

    public ReportService(IOpsFlowDbContext db, ProjectAccess access, TimeProvider timeProvider)
    {
        _db = db;
        _access = access;
        _timeProvider = timeProvider;
    }

    public async Task<ReportOverviewDto> OverviewAsync(ReportQuery query, CancellationToken cancellationToken)
    {
        var today = ResolveToday(query.Today);
        var todayStart = Start(today);
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        if (query.ProjectId is Guid projectId)
        {
            projects = projects.Where(p => p.Id == projectId);
        }

        var tasks = _db.TaskItems.AsNoTracking().Where(t => projects.Any(p => p.Id == t.ProjectId));
        var open = tasks.Where(t => OpenStatuses.Contains(t.Status));
        var overdue = open.Where(t => t.DueDate < todayStart);

        var byStatus = await tasks
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        var byPriority = await open
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Priority, x => x.Count, cancellationToken);

        var totals = new ReportTotalsDto(
            await projects.CountAsync(p => p.Status == ProjectStatus.Active, cancellationToken),
            OpenStatuses.Sum(status => byStatus.GetValueOrDefault(status)),
            await overdue.CountAsync(cancellationToken),
            await open.CountAsync(t => t.AssigneeId == null, cancellationToken),
            await tasks.CountAsync(t => t.CompletedAt >= Start(today.AddDays(-29)), cancellationToken));

        return new ReportOverviewDto(
            today,
            totals,
            Enum.GetValues<TaskItemStatus>().Select(s => new StatusCountDto(s, byStatus.GetValueOrDefault(s))).ToList(),
            Enum.GetValues<TaskPriority>().Select(p => new PriorityCountDto(p, byPriority.GetValueOrDefault(p))).ToList(),
            await WeeklyAsync(tasks, today, cancellationToken),
            await WorkloadAsync(open, todayStart, cancellationToken),
            await ProjectsAsync(projects, todayStart, cancellationToken));
    }

    public async Task<MySummaryDto> MySummaryAsync(DateOnly? today, CancellationToken cancellationToken)
    {
        var day = ResolveToday(today);
        var userId = _access.CurrentUserId;
        var projects = await _access.VisibleProjectsAsync(cancellationToken);

        var mine = _db.TaskItems.AsNoTracking()
            .Where(t => t.AssigneeId == userId && projects.Any(p => p.Id == t.ProjectId));
        var open = mine.Where(t => OpenStatuses.Contains(t.Status));

        return new MySummaryDto(
            await open.CountAsync(cancellationToken),
            await open.CountAsync(t => t.DueDate < Start(day), cancellationToken),
            await open.CountAsync(t => t.DueDate >= Start(day) && t.DueDate < Start(day.AddDays(7)), cancellationToken),
            await mine.CountAsync(t => t.CompletedAt >= Start(day.AddDays(-6)), cancellationToken));
    }

    private static async Task<IReadOnlyList<WeeklyActivityDto>> WeeklyAsync(
        IQueryable<TaskItem> tasks,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var firstWeek = WeekStart(today).AddDays(-7 * (WeekCount - 1));
        var since = Start(firstWeek);

        var created = await tasks.Where(t => t.CreatedAt >= since).Select(t => t.CreatedAt).ToListAsync(cancellationToken);
        var completed = await tasks
            .Where(t => t.CompletedAt >= since)
            .Select(t => t.CompletedAt!.Value)
            .ToListAsync(cancellationToken);

        int Bucket(DateTimeOffset value) => (DateOnly.FromDateTime(value.UtcDateTime).DayNumber - firstWeek.DayNumber) / 7;

        var createdCounts = new int[WeekCount];
        var completedCounts = new int[WeekCount];

        foreach (var index in created.Select(Bucket).Where(i => i is >= 0 and < WeekCount))
        {
            createdCounts[index]++;
        }

        foreach (var index in completed.Select(Bucket).Where(i => i is >= 0 and < WeekCount))
        {
            completedCounts[index]++;
        }

        return Enumerable.Range(0, WeekCount)
            .Select(i => new WeeklyActivityDto(firstWeek.AddDays(7 * i), createdCounts[i], completedCounts[i]))
            .ToList();
    }

    private static async Task<IReadOnlyList<WorkloadDto>> WorkloadAsync(
        IQueryable<TaskItem> open,
        DateTimeOffset todayStart,
        CancellationToken cancellationToken)
    {
        var rows = await open
            .Where(t => t.AssigneeId != null)
            .GroupBy(t => new { t.Assignee!.Id, t.Assignee.FirstName, t.Assignee.LastName, t.Assignee.Email })
            .Select(g => new
            {
                g.Key,
                Open = g.Count(),
                Overdue = g.Count(t => t.DueDate < todayStart)
            })
            .OrderByDescending(x => x.Open)
            .ThenBy(x => x.Key.FirstName)
            .Take(WorkloadLimit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new WorkloadDto(new UserReferenceDto(r.Key.Id, r.Key.FirstName, r.Key.LastName, r.Key.Email), r.Open, r.Overdue))
            .ToList();
    }

    private static async Task<IReadOnlyList<ProjectProgressDto>> ProjectsAsync(
        IQueryable<Project> projects,
        DateTimeOffset todayStart,
        CancellationToken cancellationToken)
    {
        var rows = await projects
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Key,
                p.Name,
                p.Status,
                Total = p.Tasks.Count(t => t.Status != TaskItemStatus.Cancelled),
                Done = p.Tasks.Count(t => t.Status == TaskItemStatus.Done),
                Open = p.Tasks.Count(t => t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Cancelled),
                Overdue = p.Tasks.Count(t => t.Status != TaskItemStatus.Done && t.Status != TaskItemStatus.Cancelled && t.DueDate < todayStart)
            })
            .OrderByDescending(p => p.Open)
            .ThenBy(p => p.Name)
            .Take(ProjectLimit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(p => new ProjectProgressDto(p.Id, p.Key, p.Name, p.Status, p.Total, p.Done, p.Open, p.Overdue))
            .ToList();
    }

    private DateOnly ResolveToday(DateOnly? today)
    {
        var utcToday = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        return today is DateOnly value && Math.Abs(value.DayNumber - utcToday.DayNumber) <= 1 ? value : utcToday;
    }

    private static DateOnly WeekStart(DateOnly date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    private static DateTimeOffset Start(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
