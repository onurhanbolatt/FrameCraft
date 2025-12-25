using FluentValidation;

namespace FrameCraft.Application.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id zorunludur");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("İsim zorunludur")
            .MaximumLength(200).WithMessage("İsim en fazla 200 karakter olabilir")
            .MinimumLength(2).WithMessage("İsim en az 2 karakter olmalıdır");

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage("Telefon en fazla 20 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçersiz email formatı")
            .MaximumLength(100).WithMessage("Email en fazla 100 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Adres en fazla 500 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Address));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notlar en fazla 1000 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}
