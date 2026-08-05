using Core.Entities;
using FluentValidation;

namespace Core.Validations.PurchaseOrders;

/// <summary>PurchaseOrder entity doğrulama kuralları.</summary>
public class PurchaseOrderValidator : AbstractValidator<PurchaseOrder>
{
    public PurchaseOrderValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Tedarikçi zorunludur.");

        RuleFor(x => x.WarehouseId)
            .NotEmpty().WithMessage("Teslimat deposu zorunludur.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı zorunludur.");

        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Sipariş numarası zorunludur.")
            .MaximumLength(100).WithMessage("Sipariş numarası en fazla 100 karakter olabilir.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam tutar negatif olamaz.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçersiz sipariş durumu.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Sipariş en az bir kalem içermelidir.");

        RuleForEach(x => x.Items)
            .SetValidator(new PurchaseOrderItemValidator());
    }
}
