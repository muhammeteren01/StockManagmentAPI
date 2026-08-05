using Core.Entities;
using FluentValidation;

namespace Core.Validations.Suppliers;

/// <summary>Supplier entity doğrulama kuralları.</summary>
public class SupplierValidator : AbstractValidator<Supplier>
{
    public SupplierValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Tedarikçi firma adı zorunludur.")
            .MaximumLength(200).WithMessage("Firma adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("İlgili kişi adı zorunludur.")
            .MaximumLength(150).WithMessage("İlgili kişi en fazla 150 karakter olabilir.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon zorunludur.")
            .MaximumLength(50).WithMessage("Telefon en fazla 50 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta giriniz.")
            .MaximumLength(255).WithMessage("E-posta en fazla 255 karakter olabilir.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres zorunludur.");

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi no zorunludur.")
            .MaximumLength(50).WithMessage("Vergi no en fazla 50 karakter olabilir.");
    }
}
