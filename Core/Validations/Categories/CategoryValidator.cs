using Core.Entities;
using FluentValidation;

namespace Core.Validations.Categories;

/// <summary>Category entity doğrulama kuralları.</summary>
public class CategoryValidator : AbstractValidator<Category>
{
    public CategoryValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kategori adı zorunludur.")
            .MaximumLength(150).WithMessage("Kategori adı en fazla 150 karakter olabilir.");
    }
}
