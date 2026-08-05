using Core.DTOs.Categories;
using FluentValidation;

namespace Core.Validations.Categories;

/// <summary>CreateCategoryRequest doğrulama kuralları.</summary>
public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}

/// <summary>UpdateCategoryRequest doğrulama kuralları.</summary>
public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
    }
}
