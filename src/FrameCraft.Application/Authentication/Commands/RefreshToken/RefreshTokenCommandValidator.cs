using FluentValidation;

namespace FrameCraft.Application.Authentication.Commands.RefreshToken;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token boş olamaz")
            .MinimumLength(20)
            .WithMessage("Geçersiz refresh token formatı");
    }
}
