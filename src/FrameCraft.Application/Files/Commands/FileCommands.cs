using FluentValidation;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Files.DTOs;
using FrameCraft.Domain.Entities.Storage;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Files.Commands;

// ============================================
// Upload Single File Command
// ============================================

public record UploadFileCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    string? Folder = null,
    string? Description = null,
    Guid? EntityId = null,
    string? EntityType = null,
    string? Category = null) : IRequest<FileUploadResponse>;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream is required");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required")
            .MaximumLength(255).WithMessage("File name cannot exceed 255 characters")
            .Must(FileValidationRules.IsAllowed).WithMessage("File type is not allowed");

        RuleFor(x => x.FileSize)
            .GreaterThan(0).WithMessage("File cannot be empty")
            .LessThanOrEqualTo(FileValidationRules.MaxFileSizeBytes)
                .WithMessage($"File size cannot exceed {FileValidationRules.MaxFileSizeMB}MB");

        RuleFor(x => x.ContentType)
            .NotEmpty().WithMessage("Content type is required");

        RuleFor(x => x.Folder)
            .MaximumLength(200).WithMessage("Folder path cannot exceed 200 characters")
            .Matches(@"^[a-zA-Z0-9\-_/]*$").WithMessage("Folder can only contain letters, numbers, dashes, underscores, and slashes")
            .When(x => !string.IsNullOrEmpty(x.Folder));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.EntityType)
            .MaximumLength(100).WithMessage("Entity type cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.EntityType));

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));
    }
}

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, FileUploadResponse>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadFileCommandHandler> _logger;

    public UploadFileCommandHandler(
        IFileStorageService fileStorageService,
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UploadFileCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<FileUploadResponse> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Uploading file: {FileName}, Size: {Size}, Folder: {Folder}",
            request.FileName, request.FileSize, request.Folder);

        // Upload to S3
        var result = await _fileStorageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.FileSize,
            request.Folder,
            cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning("File upload failed: {Error}", result.ErrorMessage);
            return new FileUploadResponse(
                Success: false,
                FileId: null,
                FileKey: null,
                FileUrl: null,
                FileName: null,
                OriginalFileName: request.FileName,
                ContentType: null,
                FileSize: 0,
                ErrorMessage: result.ErrorMessage,
                UploadedAt: null);
        }

        // Save metadata to database
        var uploadedFile = UploadedFile.Create(
            fileKey: result.FileKey!,
            fileName: result.FileName!,
            originalFileName: result.OriginalFileName!,
            contentType: result.ContentType!,
            fileSize: result.FileSize,
            uploadedBy: _currentUserService.UserId,
            folder: request.Folder,
            description: request.Description,
            entityId: request.EntityId,
            entityType: request.EntityType,
            category: request.Category);

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "File uploaded and saved: {FileId}, Key: {FileKey}",
            uploadedFile.Id, uploadedFile.FileKey);

        return new FileUploadResponse(
            Success: true,
            FileId: uploadedFile.Id.ToString(),
            FileKey: result.FileKey,
            FileUrl: result.FileUrl,
            FileName: result.FileName,
            OriginalFileName: result.OriginalFileName,
            ContentType: result.ContentType,
            FileSize: result.FileSize,
            ErrorMessage: null,
            UploadedAt: uploadedFile.CreatedAt);
    }
}

// ============================================
// Delete File Command
// ============================================

public record DeleteFileCommand(Guid FileId) : IRequest<bool>;

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, bool>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeleteFileCommandHandler> _logger;

    public DeleteFileCommandHandler(
        IFileStorageService fileStorageService,
        IApplicationDbContext context,
        ILogger<DeleteFileCommandHandler> logger)
    {
        _fileStorageService = fileStorageService;
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.UploadedFiles.FindAsync(new object[] { request.FileId }, cancellationToken);
        
        if (file == null)
        {
            _logger.LogWarning("File not found for deletion: {FileId}", request.FileId);
            return false;
        }

        // Delete from S3
        var deleted = await _fileStorageService.DeleteAsync(file.FileKey, cancellationToken);
        
        if (!deleted)
        {
            _logger.LogWarning("Failed to delete file from storage: {FileKey}", file.FileKey);
        }

        // Soft delete from database
        file.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File deleted: {FileId}, Key: {FileKey}", request.FileId, file.FileKey);
        return true;
    }
}

// ============================================
// Update File Command
// ============================================

public record UpdateFileCommand(
    Guid FileId,
    string? Description = null,
    int? DisplayOrder = null,
    string? Category = null) : IRequest<bool>;

public class UpdateFileCommandHandler : IRequestHandler<UpdateFileCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateFileCommandHandler> _logger;

    public UpdateFileCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateFileCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateFileCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.UploadedFiles.FindAsync(new object[] { request.FileId }, cancellationToken);
        
        if (file == null)
        {
            _logger.LogWarning("File not found for update: {FileId}", request.FileId);
            return false;
        }

        if (request.Description != null)
            file.UpdateDescription(request.Description);
            
        if (request.DisplayOrder.HasValue)
            file.SetDisplayOrder(request.DisplayOrder.Value);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File updated: {FileId}", request.FileId);
        return true;
    }
}

// ============================================
// Attach File to Entity Command
// ============================================

public record AttachFileToEntityCommand(
    Guid FileId,
    Guid EntityId,
    string EntityType,
    string? Category = null) : IRequest<bool>;

public class AttachFileToEntityCommandHandler : IRequestHandler<AttachFileToEntityCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AttachFileToEntityCommandHandler> _logger;

    public AttachFileToEntityCommandHandler(
        IApplicationDbContext context,
        ILogger<AttachFileToEntityCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(AttachFileToEntityCommand request, CancellationToken cancellationToken)
    {
        var file = await _context.UploadedFiles.FindAsync(new object[] { request.FileId }, cancellationToken);
        
        if (file == null)
        {
            _logger.LogWarning("File not found: {FileId}", request.FileId);
            return false;
        }

        file.AttachToEntity(request.EntityId, request.EntityType, request.Category);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "File {FileId} attached to {EntityType}:{EntityId}", 
            request.FileId, request.EntityType, request.EntityId);
        return true;
    }
}
