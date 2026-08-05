using Core.Entities;
using FluentValidation;

namespace Core.Validations.PurchaseOrders;

/// <summary>PurchaseOrderItem entity doğrulama kuralları.</summary>
public class PurchaseOrderItemValidator : AbstractValidator<PurchaseOrderItem>
{
    public PurchaseOrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün zorunludur.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Sipariş miktarı pozitif olmalıdır.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Birim fiyat negatif olamaz.");

        RuleFor(x => x.ReceivedQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Teslim alınan miktar negatif olamaz.")
            .LessThanOrEqualTo(x => x.Quantity).WithMessage("Teslim alınan miktar sipariş miktarını aşamaz.");
    }
}
