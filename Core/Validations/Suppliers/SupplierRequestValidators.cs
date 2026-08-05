using Core.DTOs.Suppliers;
using FluentValidation;

namespace Core.Validations.Suppliers;

/// <summary>CreateSupplierRequest doğrulama kuralları.</summary>
public class CreateSupplierRequestValidator : AbstractValidator<CreateSupplierRequest>
{
    public CreateSupplierRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.TaxNumber).NotEmpty().MaximumLength(50);
    }
}

/// <summary>UpdateSupplierRequest doğrulama kuralları.</summary>
public class UpdateSupplierRequestValidator : AbstractValidator<UpdateSupplierRequest>
{
    public UpdateSupplierRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.TaxNumber).NotEmpty().MaximumLength(50);
    }
}
