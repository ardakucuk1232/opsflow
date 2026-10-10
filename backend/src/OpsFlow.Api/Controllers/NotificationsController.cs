using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Features.Notifications;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet]
    [ProducesResponseType(typeof(NotificationListDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationListDto>> List(CancellationToken cancellationToken)
    {
        return Ok(await _notifications.ListAsync(cancellationToken));
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        await _notifications.MarkAsReadAsync(id, cancellationToken);

        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notifications.MarkAllAsReadAsync(cancellationToken);

        return NoContent();
    }
}
