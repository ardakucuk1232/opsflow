using System.Text.Json;
using FluentValidation;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Projects;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Application.Features.AuditLogs;

public sealed record AuditChangeDto(string Field, JsonElement? OldValue, JsonElement? NewValue);

public sealed record AuditLogDto(
    Guid Id,
    string Action,
    string EntityName,
    string EntityId,
    string? EntityLabel,
    UserReferenceDto? User,
    IReadOnlyList<AuditChangeDto> Changes,
    string? IpAddress,
    DateTimeOffset CreatedAt);

public sealed class AuditLogQuery : PageRequest
{
    public static readonly IReadOnlyList<string> EntityNames =
    [
        "Company", "User", "Role", "RolePermission", "UserRole", "Project", "ProjectMember", "TaskItem", "TaskComment", "Attachment"
    ];

    public static readonly IReadOnlyList<string> Actions = [AuditActions.Created, AuditActions.Updated, AuditActions.Deleted];

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public Guid? UserId { get; init; }

    public string? Action { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? Search { get; init; }
}

public sealed class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.EntityName).Must(name => AuditLogQuery.EntityNames.Contains(name)).When(x => x.EntityName is not null);
        RuleFor(x => x.Action).Must(action => AuditLogQuery.Actions.Contains(action)).When(x => x.Action is not null);
        RuleFor(x => x.EntityId).MaximumLength(100);
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From is not null && x.To is not null);
    }
}
