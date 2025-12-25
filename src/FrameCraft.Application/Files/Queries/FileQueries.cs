using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Files.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.Files.Queries;

// ============================================
// Get File By Id
// ============================================

public record GetFileByIdQuery(Guid FileId, bool IncludePreSignedUrl = false) : IRequest<FileInfoResponse?>;

public class GetFileByIdQueryHandler : IRequestHandler<GetFileByIdQuery, FileInfoResponse?>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public GetFileByIdQueryHandler(
        IApplicationDbContext context,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<FileInfoResponse?> Handle(GetFileByIdQuery request, CancellationToken cancellationToken)
    {
        var file = await _context.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && !f.IsDeleted, cancellationToken);

        if (file == null) return null;

        string? preSignedUrl = null;
        if (request.IncludePreSignedUrl)
        {
            preSignedUrl = await _fileStorageService.GetPreSignedUrlAsync(file.FileKey);
        }

        return new FileInfoResponse(
            FileId: file.Id.ToString(),
            FileKey: file.FileKey,
            FileName: file.FileName,
            OriginalFileName: file.OriginalFileName,
            ContentType: file.ContentType,
            FileSize: file.FileSize,
            FileSizeFormatted: file.FileSizeFormatted,
            FileUrl: preSignedUrl ?? $"/api/files/{file.Id}/download",
            PreSignedUrl: preSignedUrl,
            UploadedAt: file.CreatedAt,
            Description: file.Description,
            UploadedBy: file.UploadedBy,
            EntityId: file.EntityId,
            EntityType: file.EntityType,
            Category: file.Category);
    }
}

// ============================================
// Get Files By Entity
// ============================================

public record GetFilesByEntityQuery(
    Guid EntityId,
    string EntityType,
    string? Category = null) : IRequest<List<FileInfoResponse>>;

public class GetFilesByEntityQueryHandler : IRequestHandler<GetFilesByEntityQuery, List<FileInfoResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetFilesByEntityQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FileInfoResponse>> Handle(
        GetFilesByEntityQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.UploadedFiles
            .AsNoTracking()
            .Where(f => f.EntityId == request.EntityId
                        && f.EntityType == request.EntityType);

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(f => f.Category == request.Category);
        }

        var files = await query
            .OrderBy(f => f.DisplayOrder)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        return files.Select(f => new FileInfoResponse(
            FileId: f.Id.ToString(),
            FileKey: f.FileKey,
            FileName: f.FileName,
            OriginalFileName: f.OriginalFileName,
            ContentType: f.ContentType,
            FileSize: f.FileSize,
            FileSizeFormatted: f.FileSizeFormatted,
            FileUrl: $"/api/files/{f.Id}/download",
            PreSignedUrl: null,
            UploadedAt: f.CreatedAt,
            Description: f.Description,
            UploadedBy: f.UploadedBy,
            EntityId: f.EntityId,
            EntityType: f.EntityType,
            Category: f.Category
        )).ToList();
    }
}

// ============================================
// Get Pre-Signed URL
// ============================================

public record GetPreSignedUrlQuery(
    Guid FileId,
    int ExpirationMinutes = 60) : IRequest<PreSignedUrlResponse?>;

public class GetPreSignedUrlQueryHandler : IRequestHandler<GetPreSignedUrlQuery, PreSignedUrlResponse?>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<GetPreSignedUrlQueryHandler> _logger;

    public GetPreSignedUrlQueryHandler(
        IApplicationDbContext context,
        IFileStorageService fileStorageService,
        ILogger<GetPreSignedUrlQueryHandler> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<PreSignedUrlResponse?> Handle(
        GetPreSignedUrlQuery request,
        CancellationToken cancellationToken)
    {
        var file = await _context.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId, cancellationToken);

        if (file == null)
        {
            _logger.LogWarning("File not found for pre-signed URL: {FileId}", request.FileId);
            return null;
        }

        var preSignedUrl = await _fileStorageService.GetPreSignedUrlAsync(
            file.FileKey,
            request.ExpirationMinutes);

        return new PreSignedUrlResponse(
            FileKey: file.FileKey,
            PreSignedUrl: preSignedUrl,
            ExpiresAt: DateTime.UtcNow.AddMinutes(request.ExpirationMinutes));
    }
}

// ============================================
// List Files (Paged)
// ============================================

public record ListFilesQuery(
    string? Folder = null,
    string? EntityType = null,
    string? Category = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedFileListResponse>;

// Alias for backward compatibility
public record GetFilesPagedQuery(
    string? Folder = null,
    string? EntityType = null,
    string? Category = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PagedFileListResponse>;

public record PagedFileListResponse(
    List<FileInfoResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasNextPage,
    bool HasPreviousPage);

public class ListFilesQueryHandler : IRequestHandler<ListFilesQuery, PagedFileListResponse>
{
    private readonly IApplicationDbContext _context;

    public ListFilesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedFileListResponse> Handle(ListFilesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UploadedFiles
            .AsNoTracking()
            .Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(request.Folder))
        {
            query = query.Where(f => f.Folder == request.Folder);
        }

        if (!string.IsNullOrEmpty(request.EntityType))
        {
            query = query.Where(f => f.EntityType == request.EntityType);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(f => f.Category == request.Category);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(f => 
                f.FileName.ToLower().Contains(searchTerm) ||
                f.OriginalFileName.ToLower().Contains(searchTerm) ||
                (f.Description != null && f.Description.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = files.Select(f => new FileInfoResponse(
            FileId: f.Id.ToString(),
            FileKey: f.FileKey,
            FileName: f.FileName,
            OriginalFileName: f.OriginalFileName,
            ContentType: f.ContentType,
            FileSize: f.FileSize,
            FileSizeFormatted: f.FileSizeFormatted,
            FileUrl: $"/api/files/{f.Id}/download",
            PreSignedUrl: null,
            UploadedAt: f.CreatedAt,
            Description: f.Description,
            UploadedBy: f.UploadedBy,
            EntityId: f.EntityId,
            EntityType: f.EntityType,
            Category: f.Category
        )).ToList();

        return new PagedFileListResponse(
            Items: items,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages,
            HasNextPage: request.PageNumber < totalPages,
            HasPreviousPage: request.PageNumber > 1);
    }
}

// Handler for GetFilesPagedQuery alias
public class GetFilesPagedQueryHandler : IRequestHandler<GetFilesPagedQuery, PagedFileListResponse>
{
    private readonly IApplicationDbContext _context;

    public GetFilesPagedQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedFileListResponse> Handle(GetFilesPagedQuery request, CancellationToken cancellationToken)
    {
        var query = _context.UploadedFiles
            .AsNoTracking()
            .Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(request.Folder))
        {
            query = query.Where(f => f.Folder == request.Folder);
        }

        if (!string.IsNullOrEmpty(request.EntityType))
        {
            query = query.Where(f => f.EntityType == request.EntityType);
        }

        if (!string.IsNullOrEmpty(request.Category))
        {
            query = query.Where(f => f.Category == request.Category);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(f => 
                f.FileName.ToLower().Contains(searchTerm) ||
                f.OriginalFileName.ToLower().Contains(searchTerm) ||
                (f.Description != null && f.Description.ToLower().Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
        
        var files = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = files.Select(f => new FileInfoResponse(
            FileId: f.Id.ToString(),
            FileKey: f.FileKey,
            FileName: f.FileName,
            OriginalFileName: f.OriginalFileName,
            ContentType: f.ContentType,
            FileSize: f.FileSize,
            FileSizeFormatted: f.FileSizeFormatted,
            FileUrl: $"/api/files/{f.Id}/download",
            PreSignedUrl: null,
            UploadedAt: f.CreatedAt,
            Description: f.Description,
            UploadedBy: f.UploadedBy,
            EntityId: f.EntityId,
            EntityType: f.EntityType,
            Category: f.Category
        )).ToList();

        return new PagedFileListResponse(
            Items: items,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            TotalPages: totalPages,
            HasNextPage: request.PageNumber < totalPages,
            HasPreviousPage: request.PageNumber > 1);
    }
}
