using FluentAssertions;
using FluentValidation.TestHelper;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.Files.Commands;
using FrameCraft.Domain.Entities.Storage;
using FrameCraft.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FrameCraft.UnitTests.Files.Commands;

public class UploadFileCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidFile_UploadsSuccessfully()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var mockFileStorage = new Mock<IFileStorageService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<UploadFileCommandHandler>>();

        mockCurrentUser.Setup(x => x.UserId).Returns(_userId);

        var uploadResult = FileUploadResult.Succeeded(
            fileKey: "tenant-xxx/2024/01/test.pdf",
            fileUrl: "https://s3.amazonaws.com/bucket/test.pdf",
            fileName: "test_20240101_abc123.pdf",
            originalFileName: "test.pdf",
            contentType: "application/pdf",
            fileSize: 1024);

        mockFileStorage
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadResult);

        var handler = new UploadFileCommandHandler(
            mockFileStorage.Object,
            dbContext,
            mockCurrentUser.Object,
            mockLogger.Object);

        using var stream = new MemoryStream(new byte[1024]);
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: "test.pdf",
            ContentType: "application/pdf",
            FileSize: 1024,
            Folder: "documents",
            Description: "Test file");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.FileKey.Should().NotBeNullOrEmpty();
        result.FileName.Should().Contain("test");

        // Verify database entry
        var savedFile = await dbContext.UploadedFiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.FileKey == result.FileKey);
        savedFile.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_StorageFailure_ReturnsFailure()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: true);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var mockFileStorage = new Mock<IFileStorageService>();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<UploadFileCommandHandler>>();

        var failedResult = FileUploadResult.Failed("S3 upload failed");

        mockFileStorage
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        var handler = new UploadFileCommandHandler(
            mockFileStorage.Object,
            dbContext,
            mockCurrentUser.Object,
            mockLogger.Object);

        using var stream = new MemoryStream(new byte[1024]);
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: "test.pdf",
            ContentType: "application/pdf",
            FileSize: 1024);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("S3 upload failed");
    }

    [Fact]
    public void Validator_InvalidFileType_ShouldFail()
    {
        // Arrange
        var validator = new UploadFileCommandValidator();
        using var stream = new MemoryStream();
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: "test.exe", // Not allowed
            ContentType: "application/x-msdownload",
            FileSize: 1024);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("File type is not allowed");
    }

    [Fact]
    public void Validator_FileTooLarge_ShouldFail()
    {
        // Arrange
        var validator = new UploadFileCommandValidator();
        using var stream = new MemoryStream();
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: "test.pdf",
            ContentType: "application/pdf",
            FileSize: 100 * 1024 * 1024 + 1); // Over 100MB

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileSize);
    }

    [Fact]
    public void Validator_EmptyFileName_ShouldFail()
    {
        // Arrange
        var validator = new UploadFileCommandValidator();
        using var stream = new MemoryStream();
        var command = new UploadFileCommand(
            FileStream: stream,
            FileName: "",
            ContentType: "application/pdf",
            FileSize: 1024);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName);
    }
}

public class DeleteFileCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ExistingFile_DeletesSuccessfully()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var mockFileStorage = new Mock<IFileStorageService>();
        var mockLogger = new Mock<ILogger<DeleteFileCommandHandler>>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        mockFileStorage
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new DeleteFileCommandHandler(
            mockFileStorage.Object,
            dbContext,
            mockLogger.Object);

        var command = new DeleteFileCommand(file.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var deletedFile = await dbContext.UploadedFiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == file.Id);
        deletedFile!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var mockFileStorage = new Mock<IFileStorageService>();
        var mockLogger = new Mock<ILogger<DeleteFileCommandHandler>>();

        var handler = new DeleteFileCommandHandler(
            mockFileStorage.Object,
            dbContext,
            mockLogger.Object);

        var command = new DeleteFileCommand(Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_StorageDeleteFails_StillSoftDeletes()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);

        var mockFileStorage = new Mock<IFileStorageService>();
        var mockLogger = new Mock<ILogger<DeleteFileCommandHandler>>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        mockFileStorage
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // S3 delete fails

        var handler = new DeleteFileCommandHandler(
            mockFileStorage.Object,
            dbContext,
            mockLogger.Object);

        var command = new DeleteFileCommand(file.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert - Should still soft delete from DB
        result.Should().BeTrue();

        var deletedFile = await dbContext.UploadedFiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == file.Id);
        deletedFile!.IsDeleted.Should().BeTrue();
    }
}

public class UpdateFileCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesDescription()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<UpdateFileCommandHandler>>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        var handler = new UpdateFileCommandHandler(dbContext, mockLogger.Object);

        var command = new UpdateFileCommand(
            FileId: file.Id,
            Description: "Updated description",
            DisplayOrder: 5);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedFile = await dbContext.UploadedFiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == file.Id);
        updatedFile!.Description.Should().Be("Updated description");
        updatedFile.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public async Task Handle_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<UpdateFileCommandHandler>>();

        var handler = new UpdateFileCommandHandler(dbContext, mockLogger.Object);

        var command = new UpdateFileCommand(
            FileId: Guid.NewGuid(),
            Description: "New description");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}

public class AttachFileToEntityCommandTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidAttachment_AttachesFileToEntity()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<AttachFileToEntityCommandHandler>>();

        var file = TestDataBuilder.CreateUploadedFile(_tenantId);
        file.TenantId = _tenantId;
        dbContext.UploadedFiles.Add(file);
        await dbContext.SaveChangesAsync();

        var handler = new AttachFileToEntityCommandHandler(dbContext, mockLogger.Object);

        var entityId = Guid.NewGuid();
        var command = new AttachFileToEntityCommand(
            FileId: file.Id,
            EntityId: entityId,
            EntityType: "Customer",
            Category: "Documents");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        var updatedFile = await dbContext.UploadedFiles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == file.Id);
        updatedFile!.EntityId.Should().Be(entityId);
        updatedFile.EntityType.Should().Be("Customer");
        updatedFile.Category.Should().Be("Documents");
    }

    [Fact]
    public async Task Handle_NonExistentFile_ReturnsFalse()
    {
        // Arrange
        var tenantContext = TestDbContextFactory.CreateTenantContextMock(_tenantId, filteringEnabled: false);
        using var dbContext = TestDbContextFactory.Create(tenantContext.Object);
        var mockLogger = new Mock<ILogger<AttachFileToEntityCommandHandler>>();

        var handler = new AttachFileToEntityCommandHandler(dbContext, mockLogger.Object);

        var command = new AttachFileToEntityCommand(
            FileId: Guid.NewGuid(),
            EntityId: Guid.NewGuid(),
            EntityType: "Customer");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
