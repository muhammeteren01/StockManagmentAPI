using Core.DTOs.StockTransfers;
using FluentValidation;

namespace Core.Validations.StockTransfers;

/// <summary>CreateStockTransferItemRequest doğrulama kuralları.</summary>
public class CreateStockTransferItemRequestValidator : AbstractValidator<CreateStockTransferItemRequest>
{
    public CreateStockTransferItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

/// <summary>CreateStockTransferRequest doğrulama kuralları.</summary>
public class CreateStockTransferRequestValidator : AbstractValidator<CreateStockTransferRequest>
{
    public CreateStockTransferRequestValidator()
    {
        RuleFor(x => x.FromWarehouseId).NotEmpty();
        RuleFor(x => x.ToWarehouseId).NotEmpty()
            .NotEqual(x => x.FromWarehouseId).WithMessage("Kaynak ve hedef depo aynı olamaz.");
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ReferenceNo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreateStockTransferItemRequestValidator());
    }
}
