using Core.DTOs.StockTransactions;
using FluentValidation;

namespace Core.Validations.StockTransactions;

/// <summary>CreateStockTransactionRequest doğrulama kuralları.</summary>
public class CreateStockTransactionRequestValidator : AbstractValidator<CreateStockTransactionRequest>
{
    public CreateStockTransactionRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.TransactionType).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ReasonCode).IsInEnum().When(x => x.ReasonCode.HasValue);
        RuleFor(x => x.ReferenceNo).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.ReferenceNo));
    }
}
