using FluentValidation;

namespace FrameCraft.Application.Users.Commands.UpdatePassword;

public class UpdatePasswordCommandValidator : AbstractValidator<UpdatePasswordCommand>
{
    public UpdatePasswordCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID gereklidir");

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut şifre gereklidir")
            .When(x => !x.IsAdminReset);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni şifre gereklidir")
            .MinimumLength(6).WithMessage("Yeni şifre en az 6 karakter olmalıdır")
            .MaximumLength(100).WithMessage("Yeni şifre en fazla 100 karakter olabilir");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Şifre tekrarı gereklidir")
            .Equal(x => x.NewPassword).WithMessage("Şifreler eşleşmiyor");
    }
}
