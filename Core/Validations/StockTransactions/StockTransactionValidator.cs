using Core.Entities;
using FluentValidation;

namespace Core.Validations.StockTransactions;

/// <summary>StockTransaction entity doğrulama kuralları.</summary>
public class StockTransactionValidator : AbstractValidator<StockTransaction>
{
    public StockTransactionValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ürün zorunludur.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Depo zorunludur.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı zorunludur.");

        RuleFor(x => x.TransactionType)
            .IsInEnum().WithMessage("Geçersiz hareket tipi.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miktar her zaman pozitif olmalıdır.");

        RuleFor(x => x.ReasonCode)
            .IsInEnum().WithMessage("Geçersiz neden kodu.")
            .When(x => x.ReasonCode.HasValue);

        RuleFor(x => x.ReferenceNo)
            .MaximumLength(100).WithMessage("Referans no en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.ReferenceNo));
    }
}
