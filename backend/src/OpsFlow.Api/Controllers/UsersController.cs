using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Common.Pagination;
using OpsFlow.Application.Features.Users;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [HasPermission(PermissionCodes.UserView)]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<UserSummaryDto>>> List(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _userService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionCodes.UserView)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserSummaryDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _userService.GetAsync(id, cancellationToken));
    }

    [HttpPost("invitations")]
    [HasPermission(PermissionCodes.UserInvite)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserSummaryDto>> Invite(
        InviteUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.InviteAsync(request, cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
    }

    [HttpPost("{id:guid}/resend-invitation")]
    [HasPermission(PermissionCodes.UserInvite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResendInvitation(Guid id, CancellationToken cancellationToken)
    {
        await _userService.ResendInvitationAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [HasPermission(PermissionCodes.UserManage)]
    [ProducesResponseType(typeof(UserSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserSummaryDto>> UpdateRoles(
        Guid id,
        UpdateUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _userService.UpdateRolesAsync(id, request, cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(PermissionCodes.UserManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeactivateAsync(id, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(PermissionCodes.UserManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _userService.ActivateAsync(id, cancellationToken);

        return NoContent();
    }
}
