using Core.Entities;
using Core.Enums;
using FluentValidation;

namespace Core.Validations.PurchaseOrders;

/// <summary>PurchaseOrder entity doğrulama kuralları.</summary>
public class PurchaseOrderValidator : AbstractValidator<PurchaseOrder>
{
    public PurchaseOrderValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("Geçersiz belge türü.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı zorunludur.");

        RuleFor(x => x.OrderNumber)
            .NotEmpty().WithMessage("Belge / sipariş numarası zorunludur.")
            .MaximumLength(100).WithMessage("Belge / sipariş numarası en fazla 100 karakter olabilir.");

        RuleFor(x => x.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Toplam tutar negatif olamaz.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçersiz durum.");

        When(x => x.DocumentType == PurchaseOrderDocumentType.PurchaseOrder, () =>
        {
            RuleFor(x => x.SupplierId)
                .NotEmpty().WithMessage("Tedarikçi zorunludur.");
            RuleFor(x => x.WarehouseId)
                .NotEmpty().WithMessage("Teslimat deposu zorunludur.");
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Sipariş en az bir kalem içermelidir.");
        });

        When(x => x.DocumentType is PurchaseOrderDocumentType.IncomingDespatch
            or PurchaseOrderDocumentType.OutgoingDespatch, () =>
        {
            RuleFor(x => x.Direction)
                .NotNull().WithMessage("İrsaliye yönü zorunludur.")
                .IsInEnum().WithMessage("Geçersiz irsaliye yönü.");
        });

        RuleFor(x => x.ActName).MaximumLength(255);
        RuleFor(x => x.ActVknTckn).MaximumLength(20);

        RuleForEach(x => x.Items)
            .SetValidator(new PurchaseOrderItemValidator());
    }
}
