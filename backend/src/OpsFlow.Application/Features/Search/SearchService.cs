using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Persistence;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Search;

public sealed class SearchService : ISearchService
{
    public const int MinimumLength = 2;
    public const int MaximumLength = 100;

    private const int ProjectLimit = 5;
    private const int TaskLimit = 8;

    private readonly IOpsFlowDbContext _db;
    private readonly ProjectAccess _access;

    public SearchService(IOpsFlowDbContext db, ProjectAccess access)
    {
        _db = db;
        _access = access;
    }

    public async Task<SearchResultDto> SearchAsync(string? term, CancellationToken cancellationToken)
    {
        term = term?.Trim() ?? string.Empty;

        if (term.Length is < MinimumLength or > MaximumLength)
        {
            throw new ValidationException(
                [new ValidationFailure("q", $"The search term must be between {MinimumLength} and {MaximumLength} characters.")]);
        }

        var visible = await _access.VisibleProjectsAsync(cancellationToken);
        var pattern = SqlText.ContainsPattern(term);

        var projects = await visible
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name.ToLower(), SqlText.Lower(pattern), SqlText.EscapeCharacter)
                || EF.Functions.Like(p.Key.ToLower(), SqlText.Lower(pattern), SqlText.EscapeCharacter))
            .OrderBy(p => p.Status == ProjectStatus.Completed || p.Status == ProjectStatus.Cancelled)
            .ThenBy(p => p.Name)
            .Take(ProjectLimit)
            .Select(p => new ProjectHitDto(p.Id, p.Key, p.Name, p.Status))
            .ToListAsync(cancellationToken);

        var tasks = await _db.TaskItems
            .AsNoTracking()
            .Where(t => visible.Any(p => p.Id == t.ProjectId))
            .Matching(term)
            .OrderBy(t => t.Status == TaskItemStatus.Done || t.Status == TaskItemStatus.Cancelled)
            .ThenByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Take(TaskLimit)
            .Select(t => new TaskHitDto(
                t.Id,
                t.ProjectId,
                t.Project.Name,
                t.Project.Key + "-" + t.Number,
                t.Title,
                t.Status))
            .ToListAsync(cancellationToken);

        return new SearchResultDto(projects, tasks);
    }
}
