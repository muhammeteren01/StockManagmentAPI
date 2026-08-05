using Core.DTOs.PurchaseOrders;
using FluentValidation;

namespace Core.Validations.PurchaseOrders;

/// <summary>CreatePurchaseOrderItemRequest doğrulama kuralları.</summary>
public class CreatePurchaseOrderItemRequestValidator : AbstractValidator<CreatePurchaseOrderItemRequest>
{
    public CreatePurchaseOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
    }
}

/// <summary>CreatePurchaseOrderRequest doğrulama kuralları.</summary>
public class CreatePurchaseOrderRequestValidator : AbstractValidator<CreatePurchaseOrderRequest>
{
    public CreatePurchaseOrderRequestValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.OrderNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new CreatePurchaseOrderItemRequestValidator());
    }
}
