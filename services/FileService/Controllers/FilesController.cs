using FileService.Application.Commands;
using FileService.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

// NOTE: Auth is intentionally not wired up for this pass (explicit product decision) — endpoints are open.
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    public FilesController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = new { message = "No file uploaded.", code = "EMPTY_FILE" } });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var uploadedBy = User.FindFirst("sub")?.Value ?? User.Identity?.Name;

        var command = new UploadFileCommand(ms.ToArray(), file.FileName, file.ContentType, uploadedBy);
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetMetadata), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetFileQuery(id), ct);
        return dto is null ? NotFound() : File(dto.Content, dto.ContentType, dto.FileName);
    }

    [HttpGet("{id:guid}/metadata")]
    public async Task<IActionResult> GetMetadata(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetFileMetadataQuery(id), ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var (items, total) = await _mediator.Send(new ListFilesQuery(page, pageSize), ct);
        return Ok(new { data = items, pagination = new { total, page, pageSize } });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFileCommand(id), ct);
        return Ok(new { success = true, message = "File deleted successfully." });
    }

    [HttpPost("{id:guid}/edit-link")]
    public async Task<IActionResult> GetEditLink(Guid id, CancellationToken ct)
    {
        var editUrl = await _mediator.Send(new GetEditLinkCommand(id), ct);
        return Ok(new { editUrl });
    }

    [HttpPost("{id:guid}/sync-from-drive")]
    public async Task<IActionResult> SyncFromDrive(Guid id, CancellationToken ct)
    {
        var dto = await _mediator.Send(new SyncFromDriveCommand(id), ct);
        return Ok(dto);
    }
}
