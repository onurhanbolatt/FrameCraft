using FluentValidation;

namespace FrameCraft.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("Kullanıcı ID gereklidir");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email adresi gereklidir")
            .EmailAddress()
            .WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Ad gereklidir")
            .MaximumLength(50)
            .WithMessage("Ad en fazla 50 karakter olabilir");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Soyad gereklidir")
            .MaximumLength(50)
            .WithMessage("Soyad en fazla 50 karakter olabilir");
    }
}