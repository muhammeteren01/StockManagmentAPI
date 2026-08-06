using Core.Abstractions;
using Core.DTOs.StockTransactions;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.StockTransactions;
using Moq;
using Service.Services;

namespace API.Tests.Services.StockTransactions;

/// <summary>StockTransactionService birim testleri için ortak kurulum.</summary>
internal static class StockTransactionServiceTestHelper
{
    public static StockTransactionService CreateSut(
        Mock<IStockTransactionRepository> transactionRepository,
        Mock<IInventoryRepository> inventoryRepository,
        Mock<IProductRepository> productRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            transactionRepository.Object,
            inventoryRepository.Object,
            productRepository.Object,
            warehouseRepository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateStockTransactionRequestValidator());

    public static CreateStockTransactionRequest ValidCreate(
        Guid? productId = null,
        Guid? warehouseId = null,
        TransactionType transactionType = TransactionType.In,
        int quantity = 10,
        ReasonCode? reasonCode = null,
        string? referenceNo = "REF-001",
        string? notes = "Giriş") =>
        new()
        {
            ProductId = productId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            TransactionType = transactionType,
            Quantity = quantity,
            ReasonCode = reasonCode,
            ReferenceNo = referenceNo,
            Notes = notes
        };

    public static Product CreateProduct(Guid id, Guid companyId, string name = "Laptop") =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            CategoryId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Sku = "SKU-001",
            Name = name,
            Description = "Açıklama",
            UnitPrice = 100m,
            SellingPrice = 120m,
            MinStockLevel = 1,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

    public static Warehouse CreateWarehouse(Guid id, Guid companyId, string name = "Ana Depo") =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            Name = name,
            Location = "İstanbul",
            IsActive = true
        };

    public static Inventory CreateInventory(
        Guid companyId,
        Guid productId,
        Guid warehouseId,
        int quantity = 100) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            LastUpdated = DateTime.UtcNow,
            RowVersion = [1, 2, 3, 4, 5, 6, 7, 8]
        };

    public static StockTransaction CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        Guid? productId = null,
        Guid? warehouseId = null,
        Guid? userId = null,
        TransactionType transactionType = TransactionType.In,
        int quantity = 10) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            ProductId = productId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            TransactionType = transactionType,
            Quantity = quantity,
            TransactionDate = DateTime.UtcNow
        };

    /// <summary>Aynı şirkete ait ürün ve depoyu mock'lar.</summary>
    public static void SetupSameCompanyProductAndWarehouse(
        Mock<IProductRepository> productRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Guid companyId,
        Guid productId,
        Guid warehouseId)
    {
        productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct(productId, companyId));
        warehouseRepository
            .Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWarehouse(warehouseId, companyId));
    }

    public static void SetupCompanyAdminCurrentUser(
        Mock<ICurrentUser> currentUser, Guid companyId, Guid? userId = null)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        currentUser.SetupGet(c => c.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.SetupGet(c => c.Role).Returns(UserRole.CompanyAdmin);
    }

    public static void SetupSuperAdminCurrentUser(Mock<ICurrentUser> currentUser, Guid? userId = null)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(true);
        currentUser.SetupGet(c => c.CompanyId).Returns((Guid?)null);
        currentUser.SetupGet(c => c.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.SetupGet(c => c.Role).Returns(UserRole.SuperAdmin);
    }

    public static void SetupStaffCurrentUser(
        Mock<ICurrentUser> currentUser, Guid companyId, Guid? userId = null)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        currentUser.SetupGet(c => c.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.SetupGet(c => c.Role).Returns(UserRole.Staff);
    }
}
