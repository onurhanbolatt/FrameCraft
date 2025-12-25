using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Files.Commands;
using FrameCraft.Application.Files.DTOs;
using FrameCraft.Application.Files.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FrameCraft.API.Controllers.Storage;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FilesController> _logger;

    public FilesController(
        IMediator mediator,
        IFileStorageService fileStorageService,
        ILogger<FilesController> logger)
    {
        _mediator = mediator;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single file
    /// </summary>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> Upload(
        IFormFile file,
        [FromForm] string? folder = null,
        [FromForm] string? description = null,
        [FromForm] Guid? entityId = null,
        [FromForm] string? entityType = null,
        [FromForm] string? category = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new FileUploadResponse(
                Success: false,
                FileId: null,
                FileKey: null,
                FileUrl: null,
                FileName: null,
                OriginalFileName: null,
                ContentType: null,
                FileSize: 0,
                ErrorMessage: "File is required",
                UploadedAt: null));
        }

        // Validate file
        var validationError = FileValidationRules.Validate(file.FileName, file.Length, file.ContentType);
        if (validationError != null)
        {
            return BadRequest(new FileUploadResponse(
                Success: false,
                FileId: null,
                FileKey: null,
                FileUrl: null,
                FileName: null,
                OriginalFileName: file.FileName,
                ContentType: null,
                FileSize: 0,
                ErrorMessage: validationError,
                UploadedAt: null));
        }

        await using var stream = file.OpenReadStream();
        
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: file.FileName,
            ContentType: file.ContentType,
            FileSize: file.Length,
            Folder: folder,
            Description: description,
            EntityId: entityId,
            EntityType: entityType,
            Category: category);

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Upload multiple files
    /// </summary>
    [HttpPost("upload-multiple")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB total
    [ProducesResponseType(typeof(MultipleFilesUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MultipleFilesUploadResponse>> UploadMultiple(
        IFormFileCollection files,
        [FromForm] string? folder = null,
        [FromForm] Guid? entityId = null,
        [FromForm] string? entityType = null,
        [FromForm] string? category = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        if (files.Count > 10)
        {
            return BadRequest("Cannot upload more than 10 files at once");
        }

        var results = new List<FileUploadResponse>();

        foreach (var file in files)
        {
            var validationError = FileValidationRules.Validate(file.FileName, file.Length, file.ContentType);
            if (validationError != null)
            {
                results.Add(new FileUploadResponse(
                    Success: false,
                    FileId: null,
                    FileKey: null,
                    FileUrl: null,
                    FileName: null,
                    OriginalFileName: file.FileName,
                    ContentType: null,
                    FileSize: 0,
                    ErrorMessage: validationError,
                    UploadedAt: null));
                continue;
            }

            await using var stream = file.OpenReadStream();
            
            var command = new UploadFileCommand(
                FileStream: stream,
                FileName: file.FileName,
                ContentType: file.ContentType,
                FileSize: file.Length,
                Folder: folder,
                EntityId: entityId,
                EntityType: entityType,
                Category: category);

            var result = await _mediator.Send(command);
            results.Add(result);
        }

        var successCount = results.Count(r => r.Success);
        var failedCount = results.Count(r => !r.Success);

        return Ok(new MultipleFilesUploadResponse(
            TotalFiles: files.Count,
            SuccessCount: successCount,
            FailedCount: failedCount,
            Results: results));
    }

    /// <summary>
    /// Get file information by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FileInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FileInfoResponse>> GetById(
        Guid id,
        [FromQuery] bool includePreSignedUrl = false)
    {
        var query = new GetFileByIdQuery(id, includePreSignedUrl);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Download a file
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id)
    {
        var fileInfo = await _mediator.Send(new GetFileByIdQuery(id));
        
        if (fileInfo == null)
        {
            return NotFound();
        }

        var stream = await _fileStorageService.DownloadAsync(fileInfo.FileKey);
        
        if (stream == null)
        {
            _logger.LogWarning("File not found in storage: {FileKey}", fileInfo.FileKey);
            return NotFound();
        }

        return File(stream, fileInfo.ContentType, fileInfo.OriginalFileName);
    }

    /// <summary>
    /// Get pre-signed URL for direct access
    /// </summary>
    [HttpGet("{id:guid}/presigned-url")]
    [ProducesResponseType(typeof(PreSignedUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PreSignedUrlResponse>> GetPreSignedUrl(
        Guid id,
        [FromQuery] int expirationMinutes = 60)
    {
        var query = new GetPreSignedUrlQuery(id, expirationMinutes);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Get files by entity
    /// </summary>
    [HttpGet("entity/{entityId:guid}")]
    [ProducesResponseType(typeof(List<FileInfoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FileInfoResponse>>> GetByEntity(
        Guid entityId,
        [FromQuery] string entityType,
        [FromQuery] string? category = null)
    {
        var query = new GetFilesByEntityQuery(entityId, entityType, category);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// List files with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedFileListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedFileListResponse>> List(
        [FromQuery] string? folder = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? category = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new ListFilesQuery(folder, entityType, category, searchTerm, pageNumber, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Delete a file
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteFileCommand(id));

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Check if a file exists
    /// </summary>
    [HttpHead("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exists(Guid id)
    {
        var fileInfo = await _mediator.Send(new GetFileByIdQuery(id));
        
        if (fileInfo == null)
        {
            return NotFound();
        }

        Response.Headers["X-File-Size"] = fileInfo.FileSize.ToString();
        Response.Headers["X-Content-Type"] = fileInfo.ContentType;
        Response.Headers["X-File-Name"] = fileInfo.OriginalFileName;
        
        return Ok();
    }

    /// <summary>
    /// Update file metadata
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFileRequest request)
    {
        var command = new UpdateFileCommand(
            id,
            request.Description,
            request.DisplayOrder,
            request.Category);
            
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Attach file to an entity
    /// </summary>
    [HttpPost("{id:guid}/attach")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachToEntity(
        Guid id,
        [FromBody] AttachFileRequest request)
    {
        var command = new AttachFileToEntityCommand(
            id,
            request.EntityId,
            request.EntityType,
            request.Category);
            
        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}

// Request DTOs for API
public record UpdateFileRequest(
    string? Description = null,
    int? DisplayOrder = null,
    string? Category = null);

public record AttachFileRequest(
    Guid EntityId,
    string EntityType,
    string? Category = null);
