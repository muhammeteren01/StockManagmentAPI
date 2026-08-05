using Core.Entities;
using FluentValidation;

namespace Core.Validations.StockTransfers;

/// <summary>StockTransferItem entity doğrulama kuralları.</summary>
public class StockTransferItemValidator : AbstractValidator<StockTransferItem>
{
    public StockTransferItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün zorunludur.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Transfer miktarı pozitif olmalıdır.");
    }
}
