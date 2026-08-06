using Core.Abstractions;
using Core.DTOs.StockTransfers;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.StockTransfers;
using Moq;
using Service.Services;

namespace API.Tests.Services.StockTransfers;

/// <summary>StockTransferService birim testleri için ortak kurulum.</summary>
internal static class StockTransferServiceTestHelper
{
    public static StockTransferService CreateSut(
        Mock<IStockTransferRepository> transferRepository,
        Mock<IStockTransactionRepository> transactionRepository,
        Mock<IInventoryRepository> inventoryRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IProductRepository> productRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            transferRepository.Object,
            transactionRepository.Object,
            inventoryRepository.Object,
            warehouseRepository.Object,
            productRepository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreateStockTransferRequestValidator());

    public static CreateStockTransferRequest ValidCreate(
        Guid? fromWarehouseId = null,
        Guid? toWarehouseId = null,
        string referenceNo = "TR-001",
        string? notes = "Not",
        Guid? productId = null,
        int quantity = 10) =>
        new()
        {
            FromWarehouseId = fromWarehouseId ?? Guid.NewGuid(),
            ToWarehouseId = toWarehouseId ?? Guid.NewGuid(),
            ReferenceNo = referenceNo,
            Notes = notes,
            Items =
            [
                new CreateStockTransferItemRequest
                {
                    ProductId = productId ?? Guid.NewGuid(),
                    Quantity = quantity
                }
            ]
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

    public static StockTransfer CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        Guid? fromWarehouseId = null,
        Guid? toWarehouseId = null,
        Guid? userId = null,
        StockTransferStatus status = StockTransferStatus.Pending,
        string referenceNo = "TR-001",
        Guid? productId = null,
        int quantity = 10) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            FromWarehouseId = fromWarehouseId ?? Guid.NewGuid(),
            ToWarehouseId = toWarehouseId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Status = status,
            ReferenceNo = referenceNo,
            TransferDate = DateTime.UtcNow,
            Items =
            [
                new StockTransferItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId ?? Guid.NewGuid(),
                    Quantity = quantity
                }
            ]
        };

    public static void SetupSameCompanyWarehousesAndProduct(
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IProductRepository> productRepository,
        Guid companyId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        Guid productId)
    {
        warehouseRepository
            .Setup(r => r.GetByIdAsync(fromWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWarehouse(fromWarehouseId, companyId, "Kaynak"));
        warehouseRepository
            .Setup(r => r.GetByIdAsync(toWarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWarehouse(toWarehouseId, companyId, "Hedef"));
        productRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProduct(productId, companyId));
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

    public static void SetupManagerCurrentUser(
        Mock<ICurrentUser> currentUser, Guid companyId, Guid? userId = null)
    {
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.SetupGet(c => c.IsSuperAdmin).Returns(false);
        currentUser.SetupGet(c => c.CompanyId).Returns(companyId);
        currentUser.SetupGet(c => c.UserId).Returns(userId ?? Guid.NewGuid());
        currentUser.SetupGet(c => c.Role).Returns(UserRole.Manager);
    }
}
