using FluentValidation;

namespace FrameCraft.Application.Tenants.Commands.UpdateTenant;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    public UpdateTenantCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Tenant ID gereklidir");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tenant adı gereklidir")
            .MaximumLength(100)
            .WithMessage("Tenant adı en fazla 100 karakter olabilir");

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .WithMessage("Subdomain gereklidir")
            .MaximumLength(50)
            .WithMessage("Subdomain en fazla 50 karakter olabilir")
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Subdomain sadece küçük harf, rakam ve tire içerebilir");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrEmpty(x.Email))
            .WithMessage("Geçerli bir email adresi giriniz");

        RuleFor(x => x.MaxUsers)
            .GreaterThan(0)
            .WithMessage("Maksimum kullanıcı sayısı 0'dan büyük olmalıdır");

        RuleFor(x => x.StorageQuotaMB)
            .GreaterThan(0)
            .WithMessage("Depolama kotası 0'dan büyük olmalıdır");
    }
}