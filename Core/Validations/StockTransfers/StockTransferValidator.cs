using Core.Entities;
using FluentValidation;

namespace Core.Validations.StockTransfers;

/// <summary>StockTransfer entity doğrulama kuralları.</summary>
public class StockTransferValidator : AbstractValidator<StockTransfer>
{
    public StockTransferValidator()
    {
        RuleFor(x => x.FromWarehouseId)
            .NotEmpty().WithMessage("Kaynak depo zorunludur.");

        RuleFor(x => x.ToWarehouseId)
            .NotEmpty().WithMessage("Hedef depo zorunludur.")
            .NotEqual(x => x.FromWarehouseId).WithMessage("Kaynak ve hedef depo aynı olamaz.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı zorunludur.");

        RuleFor(x => x.ReferenceNo)
            .NotEmpty().WithMessage("Referans no zorunludur.")
            .MaximumLength(100).WithMessage("Referans no en fazla 100 karakter olabilir.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçersiz transfer durumu.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Transfer en az bir kalem içermelidir.");

        RuleForEach(x => x.Items)
            .SetValidator(new StockTransferItemValidator());
    }
}
