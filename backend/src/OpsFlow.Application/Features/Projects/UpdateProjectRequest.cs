using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    ProjectStatus Status,
    DateOnly? StartDate,
    DateOnly? EndDate);
