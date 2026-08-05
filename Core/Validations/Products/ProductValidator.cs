using Core.Entities;
using FluentValidation;

namespace Core.Validations.Products;

/// <summary>Product entity doğrulama kuralları.</summary>
public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Şirket zorunludur.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori zorunludur.");

        RuleFor(x => x.SupplierId)
            .NotEmpty().WithMessage("Tedarikçi zorunludur.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("SKU zorunludur.")
            .MaximumLength(100).WithMessage("SKU en fazla 100 karakter olabilir.");

        RuleFor(x => x.Barcode)
            .MaximumLength(100).WithMessage("Barkod en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrEmpty(x.Barcode));

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ürün adı zorunludur.")
            .MaximumLength(200).WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Alış fiyatı negatif olamaz.");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Satış fiyatı negatif olamaz.");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum stok seviyesi negatif olamaz.");

        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Geçersiz ürün durumu.");
    }
}
