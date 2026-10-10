using Microsoft.AspNetCore.Mvc;
using OpsFlow.Api.Authorization;
using OpsFlow.Application.Features.Attachments;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Controllers;

[ApiController]
public sealed class AttachmentsController : ControllerBase
{
    private const long MaxRequestBytes = 11 * 1024 * 1024;

    private readonly IAttachmentService _attachments;

    public AttachmentsController(IAttachmentService attachments)
    {
        _attachments = attachments;
    }

    [HttpGet("api/tasks/{taskId:guid}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AttachmentDto>>> List(Guid taskId, CancellationToken cancellationToken)
    {
        return Ok(await _attachments.ListAsync(taskId, cancellationToken));
    }

    [HttpPost("api/tasks/{taskId:guid}/attachments")]
    [HasPermission(PermissionCodes.AttachmentUpload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AttachmentDto>> Upload(Guid taskId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();

        var attachment = await _attachments.UploadAsync(
            taskId,
            new UploadAttachmentRequest(file.FileName, file.Length, content),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, attachment);
    }

    [HttpGet("api/attachments/{id:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var content = await _attachments.OpenAsync(id, cancellationToken);

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";

        return File(content.Content, content.ContentType, content.FileName);
    }

    [HttpDelete("api/attachments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _attachments.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
