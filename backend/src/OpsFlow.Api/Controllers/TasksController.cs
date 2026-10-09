using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Features.Tasks;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ITaskCommentService _commentService;

    public TasksController(ITaskService taskService, ITaskCommentService commentService)
    {
        _taskService = taskService;
        _commentService = commentService;
    }

    [HttpGet("assigned-to-me")]
    [ProducesResponseType(typeof(IReadOnlyList<AssignedTaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssignedTaskDto>>> AssignedToMe(CancellationToken cancellationToken)
    {
        return Ok(await _taskService.ListAssignedToMeAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.GetAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionCodes.TaskUpdate)]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> Update(Guid id, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.UpdateAsync(id, request, cancellationToken));
    }

    [HttpPut("{id:guid}/assignee")]
    [HasPermission(PermissionCodes.TaskAssign)]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TaskDetailDto>> Assign(Guid id, AssignTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.AssignAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/move")]
    [HasPermission(PermissionCodes.TaskUpdate)]
    [ProducesResponseType(typeof(TaskDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> Move(Guid id, MoveTaskRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _taskService.MoveAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionCodes.TaskDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    [ProducesResponseType(typeof(IReadOnlyList<TaskCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TaskCommentDto>>> Comments(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _commentService.ListAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/comments")]
    [HasPermission(PermissionCodes.CommentCreate)]
    [ProducesResponseType(typeof(TaskCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskCommentDto>> AddComment(
        Guid id,
        AddTaskCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await _commentService.AddAsync(id, request, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpDelete("{id:guid}/comments/{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(Guid id, Guid commentId, CancellationToken cancellationToken)
    {
        await _commentService.DeleteAsync(id, commentId, cancellationToken);

        return NoContent();
    }
}
