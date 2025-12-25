using FluentValidation;

namespace FrameCraft.Application.Tenants.Commands.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İşletme adı gereklidir")
            .MaximumLength(200).WithMessage("İşletme adı en fazla 200 karakter olabilir");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain gereklidir")
            .MaximumLength(50).WithMessage("Subdomain en fazla 50 karakter olabilir")
            .Matches(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
            .WithMessage("Subdomain sadece küçük harf, rakam ve tire içerebilir. Tire ile başlayamaz veya bitemez.");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon numarası en fazla 20 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz")
            .MaximumLength(100).WithMessage("E-posta en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.MaxUsers)
            .GreaterThan(0).WithMessage("Maksimum kullanıcı sayısı en az 1 olmalıdır")
            .LessThanOrEqualTo(1000).WithMessage("Maksimum kullanıcı sayısı 1000'i geçemez");

        RuleFor(x => x.StorageQuotaMB)
            .GreaterThan(0).WithMessage("Depolama kotası en az 1 MB olmalıdır")
            .LessThanOrEqualTo(1000000).WithMessage("Depolama kotası 1 TB'ı geçemez");

        // Admin kullanıcı bilgileri doğrulaması
        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin şifresi gereklidir")
            .MinimumLength(6).WithMessage("Admin şifresi en az 6 karakter olmalıdır")
            .When(x => !string.IsNullOrEmpty(x.AdminEmail));

        RuleFor(x => x.AdminEmail)
            .EmailAddress().WithMessage("Geçerli bir admin e-posta adresi giriniz")
            .When(x => !string.IsNullOrEmpty(x.AdminEmail));

        RuleFor(x => x.AdminFirstName)
            .NotEmpty().WithMessage("Admin adı gereklidir")
            .MaximumLength(50).WithMessage("Admin adı en fazla 50 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.AdminEmail));

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Admin soyadı gereklidir")
            .MaximumLength(50).WithMessage("Admin soyadı en fazla 50 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.AdminEmail));
    }
}
