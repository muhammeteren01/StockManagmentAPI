using Core.Entities;
using FluentValidation;

namespace Core.Validations.Inventories;

/// <summary>Inventory entity doğrulama kuralları.</summary>
public class InventoryValidator : AbstractValidator<Inventory>
{
    public InventoryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün zorunludur.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Depo zorunludur.");

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stok miktarı negatif olamaz.");
    }
}
