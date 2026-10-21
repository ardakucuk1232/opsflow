using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Common.Interfaces;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Common.Persistence;
using OpsFlow.Application.Features.Projects;

namespace OpsFlow.Application.Features.AuditLogs;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IOpsFlowDbContext _db;
    private readonly IValidator<AuditLogQuery> _validator;

    public AuditLogService(IOpsFlowDbContext db, IValidator<AuditLogQuery> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var logs = _db.AuditLogs.AsNoTracking();

        if (query.EntityName is not null)
        {
            logs = logs.Where(a => a.EntityName == query.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            var entityId = query.EntityId.Trim();
            logs = logs.Where(a => a.EntityId == entityId);
        }

        if (query.UserId is Guid userId)
        {
            logs = logs.Where(a => a.UserId == userId);
        }

        if (query.Action is not null)
        {
            logs = logs.Where(a => a.Action == query.Action);
        }

        if (query.From is DateOnly from)
        {
            var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            logs = logs.Where(a => a.CreatedAt >= start);
        }

        if (query.To is DateOnly to)
        {
            var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            logs = logs.Where(a => a.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = SqlText.ContainsPattern(query.Search.Trim());
            logs = logs.Where(a => a.EntityLabel != null
                && EF.Functions.Like(a.EntityLabel.ToLower(), SqlText.Lower(pattern), SqlText.EscapeCharacter));
        }

        var page = await logs
            .OrderByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Select(a => new
            {
                a.Id,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.EntityLabel,
                User = a.User == null
                    ? null
                    : new UserReferenceDto(a.User.Id, a.User.FirstName, a.User.LastName, a.User.Email),
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.CreatedAt
            })
            .ToPagedResultAsync(query, cancellationToken);

        var items = page.Items
            .Select(a => new AuditLogDto(
                a.Id,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.EntityLabel,
                a.User,
                Changes(a.OldValues, a.NewValues),
                a.IpAddress,
                a.CreatedAt))
            .ToList();

        return new PagedResult<AuditLogDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    private static IReadOnlyList<AuditChangeDto> Changes(string? oldValues, string? newValues)
    {
        var before = Parse(oldValues);
        var after = Parse(newValues);

        return before.Keys
            .Union(after.Keys)
            .Select(field => new AuditChangeDto(
                field,
                before.TryGetValue(field, out var oldValue) ? oldValue : null,
                after.TryGetValue(field, out var newValue) ? newValue : null))
            .ToList();
    }

    private static Dictionary<string, JsonElement> Parse(string? json) =>
        json is null ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
}
