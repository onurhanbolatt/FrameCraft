using FluentValidation;

namespace FrameCraft.Application.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("Tenant ID gereklidir");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta adresi gereklidir")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
            .MaximumLength(100).WithMessage("E-posta en fazla 100 karakter olabilir");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre gereklidir")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Şifre en fazla 100 karakter olabilir");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad gereklidir")
            .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad gereklidir")
            .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("En az bir rol seçilmelidir")
            .When(x => !x.IsSuperAdmin);
    }
}
