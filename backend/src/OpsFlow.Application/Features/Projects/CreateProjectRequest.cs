using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string Key,
    string? Description,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate);
