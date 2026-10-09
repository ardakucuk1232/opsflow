using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/tasks")]
public sealed class ProjectTasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public ProjectTasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TaskSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskSummaryDto>>> List(Guid projectId, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.ListForProjectAsync(projectId, cancellationToken));
    }

    [HttpPost]
    [HasPermission(PermissionCodes.TaskCreate)]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDetailDto>> Create(
        Guid projectId,
        CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(projectId, request, cancellationToken);

        return CreatedAtAction(nameof(TasksController.Get), "Tasks", new { id = task.Id }, task);
    }
}
