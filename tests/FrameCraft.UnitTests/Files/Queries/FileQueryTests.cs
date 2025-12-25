using FluentAssertions;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Files.Queries;
using FrameCraft.Domain.Entities.Storage;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Files.Queries;

public class GetFileByIdQueryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ExistingFile_ReturnsFileWithUrl()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockFileStorage = new Mock<IFileStorageService>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        mockFileStorage
            .Setup(x => x.GetPreSignedUrlAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync("https://s3.amazonaws.com/presigned-url");

        var handler = new GetFileByIdQueryHandler(dbContext, mockFileStorage.Object);
        var query = new GetFileByIdQuery(file.Id, IncludePreSignedUrl: true);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.FileId.Should().Be(file.Id.ToString());
        result.FileUrl.Should().Contain("presigned-url");
    }

    [Fact]
    public async Task Handle_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockFileStorage = new Mock<IFileStorageService>();

        var handler = new GetFileByIdQueryHandler(dbContext, mockFileStorage.Object);
        var query = new GetFileByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletedFile_ReturnsNull()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockFileStorage = new Mock<IFileStorageService>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        file.Delete(); // Soft delete
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        var handler = new GetFileByIdQueryHandler(dbContext, mockFileStorage.Object);
        var query = new GetFileByIdQuery(file.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull("Silinen dosyalar görünmemeli");
    }

    [Fact]
    public async Task Handle_FileFromOtherTenant_ReturnsNull()
    {
        // Arrange - Tenant A context'i oluştur
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockFileStorage = new Mock<IFileStorageService>();

        // Tenant B'nin dosyasını ekle
        var otherTenantId = Guid.NewGuid();
        var file = TestDataBuilder.CreateUploadedFile(otherTenantId);
        file.TenantId = otherTenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        var handler = new GetFileByIdQueryHandler(dbContext, mockFileStorage.Object);
        var query = new GetFileByIdQuery(file.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull("Başka tenant'ın dosyası görünmemeli");
    }
}

public class GetFilesByEntityQueryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_EntityWithFiles_ReturnsAllFiles()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var entityId = Guid.NewGuid();
        var entityType = "Customer";

        var file1 = CreateFile("file1.pdf", entityId, entityType);
        var file2 = CreateFile("file2.pdf", entityId, entityType);
        var unrelatedFile = CreateFile("other.pdf", Guid.NewGuid(), "Sale");

        dbContext.UploadedFiles.AddRange(file1, file2, unrelatedFile);
        await dbContext.SaveChangesAsync();

        var handler = new GetFilesByEntityQueryHandler(dbContext);
        var query = new GetFilesByEntityQuery(entityId, entityType);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(f => f.OriginalFileName.StartsWith("file"));
    }

    [Fact]
    public async Task Handle_EntityWithNoFiles_ReturnsEmptyList()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var handler = new GetFilesByEntityQueryHandler(dbContext);
        var query = new GetFilesByEntityQuery(Guid.NewGuid(), "Customer");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ReturnsFilteredFiles()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var entityId = Guid.NewGuid();
        var entityType = "Customer";

        var docFile = CreateFile("doc.pdf", entityId, entityType, "Documents");
        var imageFile = CreateFile("image.png", entityId, entityType, "Images");

        dbContext.UploadedFiles.AddRange(docFile, imageFile);
        await dbContext.SaveChangesAsync();

        var handler = new GetFilesByEntityQueryHandler(dbContext);
        var query = new GetFilesByEntityQuery(entityId, entityType, "Documents");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(f => f.Category == "Documents");
    }

    [Fact]
    public async Task Handle_OrdersByDisplayOrder()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var entityId = Guid.NewGuid();
        var entityType = "Customer";

        var file1 = CreateFile("third.pdf", entityId, entityType);
        file1.SetDisplayOrder(3);
        var file2 = CreateFile("first.pdf", entityId, entityType);
        file2.SetDisplayOrder(1);
        var file3 = CreateFile("second.pdf", entityId, entityType);
        file3.SetDisplayOrder(2);

        dbContext.UploadedFiles.AddRange(file1, file2, file3);
        await dbContext.SaveChangesAsync();

        var handler = new GetFilesByEntityQueryHandler(dbContext);
        var query = new GetFilesByEntityQuery(entityId, entityType);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].OriginalFileName.Should().Be("first.pdf");
        result[1].OriginalFileName.Should().Be("second.pdf");
        result[2].OriginalFileName.Should().Be("third.pdf");
    }

    private UploadedFile CreateFile(
        string fileName,
        Guid? entityId = null,
        string? entityType = null,
        string? category = null)
    {
        var file = UploadedFile.Create(
            fileKey: $"tenant-xxx/2024/01/{fileName}",
            fileName: $"{fileName}_unique",
            originalFileName: fileName,
            contentType: "application/pdf",
            fileSize: 1024,
            uploadedBy: Guid.NewGuid(),
            folder: "test",
            entityId: entityId,
            entityType: entityType,
            category: category);

        file.TenantId = _tenantId;
        return file;
    }
}

public class ListFilesQueryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        // Add 25 files
        for (int i = 1; i <= 25; i++)
        {
            var file = UploadedFile.Create(
                fileKey: $"key{i}",
                fileName: $"file{i}.pdf",
                originalFileName: $"file{i}.pdf",
                contentType: "application/pdf",
                fileSize: 1024,
                uploadedBy: Guid.NewGuid());
            file.TenantId = _tenantId;
            dbContext.UploadedFiles.Add(file);
        }
        await dbContext.SaveChangesAsync();

        var handler = new ListFilesQueryHandler(dbContext);
        var query = new ListFilesQuery(PageNumber: 2, PageSize: 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
        result.TotalPages.Should().Be(3);
        result.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_FiltersResults()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var invoice1 = CreateFileWithName("invoice_001.pdf");
        var invoice2 = CreateFileWithName("invoice_002.pdf");
        var report = CreateFileWithName("report.pdf");

        dbContext.UploadedFiles.AddRange(invoice1, invoice2, report);
        await dbContext.SaveChangesAsync();

        var handler = new ListFilesQueryHandler(dbContext);
        var query = new ListFilesQuery(SearchTerm: "invoice");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(f => f.OriginalFileName.Contains("invoice"));
    }

    private UploadedFile CreateFileWithName(string fileName)
    {
        var file = UploadedFile.Create(
            fileKey: $"key-{Guid.NewGuid():N}",
            fileName: fileName,
            originalFileName: fileName,
            contentType: "application/pdf",
            fileSize: 1024,
            uploadedBy: Guid.NewGuid());
        file.TenantId = _tenantId;
        return file;
    }
}
