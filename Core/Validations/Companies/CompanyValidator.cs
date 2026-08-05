using Core.Entities;
using FluentValidation;

namespace Core.Validations.Companies;

/// <summary>Company entity doğrulama kuralları.</summary>
public class CompanyValidator : AbstractValidator<Company>
{
    public CompanyValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Şirket adı zorunludur.")
            .MaximumLength(200).WithMessage("Şirket adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.TaxOffice)
            .MaximumLength(200).WithMessage("Vergi dairesi en fazla 200 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.TaxOffice));

        RuleFor(x => x.TaxNumber)
            .MaximumLength(50).WithMessage("Vergi no en fazla 50 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.TaxNumber));

        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Telefon en fazla 50 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Geçerli bir e-posta giriniz.")
            .MaximumLength(255).WithMessage("E-posta en fazla 255 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}
