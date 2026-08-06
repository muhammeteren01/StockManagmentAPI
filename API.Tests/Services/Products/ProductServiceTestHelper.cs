using Core.Abstractions;
using Core.DTOs.Products;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.Products;
using Moq;
using Service.Services;

namespace API.Tests.Services.Products;

/// <summary>ProductService birim testleri için ortak kurulum.</summary>
internal static class ProductServiceTestHelper
{
    public static ProductService CreateSut(
        Mock<IProductRepository> repository,
        Mock<ICategoryRepository> categoryRepository,
        Mock<ISupplierRepository> supplierRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            repository.Object,
            categoryRepository.Object,
            supplierRepository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateProductRequestValidator(),
            new UpdateProductRequestValidator());

    public static CreateProductRequest ValidCreate(
        Guid? companyId = null,
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-001",
        string? barcode = "8690000000001",
        string name = "Laptop",
        string description = "15 inç laptop",
        decimal unitPrice = 10000m,
        decimal sellingPrice = 12500m,
        int minStockLevel = 5,
        ProductStatus status = ProductStatus.Active) =>
        new()
        {
            CompanyId = companyId ?? Guid.NewGuid(),
            CategoryId = categoryId ?? Guid.NewGuid(),
            SupplierId = supplierId ?? Guid.NewGuid(),
            Sku = sku,
            Barcode = barcode,
            Name = name,
            Description = description,
            UnitPrice = unitPrice,
            SellingPrice = sellingPrice,
            MinStockLevel = minStockLevel,
            Status = status
        };

    public static UpdateProductRequest ValidUpdate(
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-002",
        string? barcode = "8690000000002",
        string name = "Güncel Laptop",
        string description = "Güncel açıklama",
        decimal unitPrice = 11000m,
        decimal sellingPrice = 13500m,
        int minStockLevel = 8,
        ProductStatus status = ProductStatus.Active) =>
        new()
        {
            CategoryId = categoryId ?? Guid.NewGuid(),
            SupplierId = supplierId ?? Guid.NewGuid(),
            Sku = sku,
            Barcode = barcode,
            Name = name,
            Description = description,
            UnitPrice = unitPrice,
            SellingPrice = sellingPrice,
            MinStockLevel = minStockLevel,
            Status = status
        };

    public static Product CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        Guid? categoryId = null,
        Guid? supplierId = null,
        string sku = "SKU-001",
        string? barcode = "8690000000001",
        string name = "Laptop",
        string description = "15 inç laptop",
        decimal unitPrice = 10000m,
        decimal sellingPrice = 12500m,
        int minStockLevel = 5,
        ProductStatus status = ProductStatus.Active) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            CategoryId = categoryId ?? Guid.NewGuid(),
            SupplierId = supplierId ?? Guid.NewGuid(),
            Sku = sku,
            Barcode = barcode,
            Name = name,
            Description = description,
            UnitPrice = unitPrice,
            SellingPrice = sellingPrice,
            MinStockLevel = minStockLevel,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

    public static Category CreateCategory(Guid id, Guid companyId) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = "Elektronik"
        };

    public static Supplier CreateSupplier(Guid id, Guid companyId) =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            CompanyName = "Acme",
            ContactName = "Ali",
            Phone = "+905551112233",
            Email = "ali@acme.com",
            Address = "İstanbul",
            TaxNumber = "1234567890",
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Aynı şirkete ait kategori ve tedarikçiyi mock'lar; SKU çakışması yok.</summary>
    public static void SetupSameCompanyReferences(
        Mock<ICategoryRepository> categoryRepository,
        Mock<ISupplierRepository> supplierRepository,
        Mock<IProductRepository> productRepository,
        Guid companyId,
        Guid categoryId,
        Guid supplierId)
    {
        categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCategory(categoryId, companyId));
        supplierRepository
            .Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSupplier(supplierId, companyId));
        productRepository
            .Setup(r => r.GetBySkuAsync(companyId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
    }

    public static void SetupCompanyAdminCurrentUser(Mock<ICurrentUser> currentUser, Guid companyId)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        currentUser.SetupGet(c => c.Role).Returns(UserRole.CompanyAdmin);
    }

    public static void SetupSuperAdminCurrentUser(Mock<ICurrentUser> currentUser)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(true);
        currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        currentUser.SetupGet(c => c.Role).Returns(UserRole.SuperAdmin);
    }
}
