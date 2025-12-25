using FluentValidation;
using FrameCraft.Application.Common.Interfaces;
using FrameCraft.Application.TenantManagement.DTOs;
using FrameCraft.Domain.Entities.Authentication;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FrameCraft.Application.TenantManagement.Commands;

// ============================================
// Update User Status Command
// ============================================

public record UpdateTenantUserStatusCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public Guid CurrentUserId { get; init; }
    public bool IsActive { get; init; }
}

public class UpdateTenantUserStatusCommandValidator : AbstractValidator<UpdateTenantUserStatusCommand>
{
    public UpdateTenantUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gerekli");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gerekli");

        RuleFor(x => x.UserId)
            .NotEqual(x => x.CurrentUserId)
            .WithMessage("Kendinizi pasif yapamazsınız");
    }
}

public class UpdateTenantUserStatusCommandHandler : IRequestHandler<UpdateTenantUserStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateTenantUserStatusCommandHandler> _logger;

    public UpdateTenantUserStatusCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateTenantUserStatusCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateTenantUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.TenantId == request.TenantId &&
                !u.IsSuperAdmin,
                cancellationToken);

        if (user == null)
        {
            return false;
        }

        user.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var status = request.IsActive ? "aktif" : "pasif";
        _logger.LogInformation(
            "Kullanıcı {Status} yapıldı: {UserId} by {AdminId}",
            status, request.UserId, request.CurrentUserId);

        return true;
    }
}

// ============================================
// Delete Tenant User Command
// ============================================

public record DeleteTenantUserCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public Guid CurrentUserId { get; init; }
}

public class DeleteTenantUserCommandValidator : AbstractValidator<DeleteTenantUserCommand>
{
    public DeleteTenantUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gerekli");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gerekli");

        RuleFor(x => x.UserId)
            .NotEqual(x => x.CurrentUserId)
            .WithMessage("Kendinizi silemezsiniz");
    }
}

public class DeleteTenantUserCommandHandler : IRequestHandler<DeleteTenantUserCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeleteTenantUserCommandHandler> _logger;

    public DeleteTenantUserCommandHandler(
        IApplicationDbContext context,
        ILogger<DeleteTenantUserCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteTenantUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.TenantId == request.TenantId &&
                !u.IsSuperAdmin,
                cancellationToken);

        if (user == null)
        {
            return false;
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Kullanıcı silindi: {UserId} by {AdminId}",
            request.UserId, request.CurrentUserId);

        return true;
    }
}

// ============================================
// Create Tenant User Command
// ============================================

public record CreateTenantUserCommand : IRequest<CreateTenantUserResult>
{
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new() { "User" };
}

public record CreateTenantUserResult(
    Guid Id,
    string Email,
    string FullName,
    List<string> Roles);

public class CreateTenantUserCommandValidator : AbstractValidator<CreateTenantUserCommand>
{
    public CreateTenantUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gerekli");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gerekli")
            .EmailAddress().WithMessage("Geçerli bir email adresi girin");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gerekli")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gerekli")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gerekli")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı");

        RuleFor(x => x.Roles)
            .Must(roles => !roles.Any(r => r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("SuperAdmin rolü atanamaz");
    }
}

public class CreateTenantUserCommandHandler : IRequestHandler<CreateTenantUserCommand, CreateTenantUserResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateTenantUserCommandHandler> _logger;

    public CreateTenantUserCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateTenantUserCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreateTenantUserResult> Handle(CreateTenantUserCommand request, CancellationToken cancellationToken)
    {
        // Email kontrolü
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            throw new Domain.Exceptions.ValidationException(
                new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Bu email adresi zaten kullanılıyor" } }
                });
        }

        // Tenant kontrolü
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new Domain.Exceptions.NotFoundException("Tenant", request.TenantId);
        }

        // User oluştur
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Email = request.Email,
            PasswordHash = passwordHash,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true,
            IsSuperAdmin = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Rolleri ata
        var assignedRoles = new List<string>();
        foreach (var roleName in request.Roles)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

            if (role != null)
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow
                });
                assignedRoles.Add(roleName);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant user oluşturuldu: {Email} -> Tenant: {TenantId}",
            request.Email, request.TenantId);

        return new CreateTenantUserResult(
            Id: user.Id,
            Email: user.Email,
            FullName: $"{user.FirstName} {user.LastName}",
            Roles: assignedRoles);
    }
}

// ============================================
// Reset Tenant User Password Command
// ============================================

public record ResetTenantUserPasswordCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string NewPassword { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
}

public class ResetTenantUserPasswordCommandValidator : AbstractValidator<ResetTenantUserPasswordCommand>
{
    public ResetTenantUserPasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gerekli");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gerekli");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gerekli")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalı");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Şifreler eşleşmiyor");
    }
}

public class ResetTenantUserPasswordCommandHandler : IRequestHandler<ResetTenantUserPasswordCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResetTenantUserPasswordCommandHandler> _logger;

    public ResetTenantUserPasswordCommandHandler(
        IApplicationDbContext context,
        ILogger<ResetTenantUserPasswordCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ResetTenantUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.TenantId == request.TenantId &&
                !u.IsSuperAdmin,
                cancellationToken);

        if (user == null)
        {
            return false;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordHash = passwordHash;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant user şifresi sıfırlandı: {UserId}",
            request.UserId);

        return true;
    }
}
// ============================================
// Update Tenant User Command
// ============================================

public record UpdateTenantUserCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public Guid CurrentUserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public List<string> Roles { get; init; } = new();
}

public class UpdateTenantUserCommandValidator : AbstractValidator<UpdateTenantUserCommand>
{
    public UpdateTenantUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gerekli");

        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gerekli");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email gerekli")
            .EmailAddress().WithMessage("Geçerli bir email adresi girin");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gerekli")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gerekli")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir");

        RuleFor(x => x.UserId)
            .NotEqual(x => x.CurrentUserId)
            .WithMessage("Kendinizi bu şekilde güncelleyemezsiniz");

        RuleFor(x => x.Roles)
            .Must(roles => !roles.Any(r => r.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("SuperAdmin rolü atanamaz");
    }
}

public class UpdateTenantUserCommandHandler : IRequestHandler<UpdateTenantUserCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateTenantUserCommandHandler> _logger;

    public UpdateTenantUserCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateTenantUserCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(UpdateTenantUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u =>
                u.Id == request.UserId &&
                u.TenantId == request.TenantId &&
                !u.IsSuperAdmin &&
                !u.IsDeleted,
                cancellationToken);

        if (user == null)
        {
            return false;
        }

        // Email değiştiyse, başka kullanıcıda var mı kontrol et
        if (user.Email != request.Email)
        {
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email && u.Id != request.UserId, cancellationToken);

            if (emailExists)
            {
                throw new Domain.Exceptions.BadRequestException("Bu email adresi başka bir kullanıcı tarafından kullanılıyor");
            }
        }

        // Kullanıcı bilgilerini güncelle
        user.Email = request.Email;
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        // Rolleri güncelle
        if (request.Roles != null && request.Roles.Any())
        {
            // Mevcut rolleri temizle
            var existingRoles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync(cancellationToken);

            _context.UserRoles.RemoveRange(existingRoles);

            // Yeni rolleri ekle
            foreach (var roleName in request.Roles)
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);

                if (role != null)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Tenant user güncellendi: {UserId} by {AdminId}",
            request.UserId, request.CurrentUserId);

        return true;
    }
}