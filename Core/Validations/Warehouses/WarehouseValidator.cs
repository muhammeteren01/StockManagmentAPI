using Core.Entities;
using FluentValidation;

namespace Core.Validations.Warehouses;

/// <summary>Warehouse entity doğrulama kuralları.</summary>
public class WarehouseValidator : AbstractValidator<Warehouse>
{
    public WarehouseValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Depo adı zorunludur.")
            .MaximumLength(150).WithMessage("Depo adı en fazla 150 karakter olabilir.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Lokasyon zorunludur.")
            .MaximumLength(255).WithMessage("Lokasyon en fazla 255 karakter olabilir.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapasite 0'dan büyük olmalıdır.")
            .When(x => x.Capacity.HasValue);
    }
}
