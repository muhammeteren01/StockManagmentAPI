using Core.DTOs.Companies;
using FluentValidation;

namespace Core.Validations.Companies;

/// <summary>CreateCompanyRequest doğrulama kuralları.</summary>
public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxOffice).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TaxOffice));
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Email));
    }
}

/// <summary>UpdateCompanyRequest doğrulama kuralları.</summary>
public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxOffice).MaximumLength(200).When(x => !string.IsNullOrEmpty(x.TaxOffice));
        RuleFor(x => x.TaxNumber).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.TaxNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(255).When(x => !string.IsNullOrEmpty(x.Email));
    }
}
