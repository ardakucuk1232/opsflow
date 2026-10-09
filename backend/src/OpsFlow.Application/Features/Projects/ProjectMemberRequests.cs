using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Features.Projects;

public sealed record AddProjectMemberRequest(Guid UserId, ProjectMemberRole Role);

public sealed record UpdateProjectMemberRequest(ProjectMemberRole Role);
