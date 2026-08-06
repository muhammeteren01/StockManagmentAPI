using Core.Abstractions;
using Core.DTOs.PurchaseOrders;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations.PurchaseOrders;
using Moq;
using Service.Services;

namespace API.Tests.Services.PurchaseOrders;

/// <summary>PurchaseOrderService birim testleri için ortak kurulum.</summary>
internal static class PurchaseOrderServiceTestHelper
{
    public static PurchaseOrderService CreateSut(
        Mock<IPurchaseOrderRepository> purchaseOrderRepository,
        Mock<IStockTransactionRepository> transactionRepository,
        Mock<IInventoryRepository> inventoryRepository,
        Mock<ISupplierRepository> supplierRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IProductRepository> productRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ICurrentUser> currentUser) =>
        new(
            purchaseOrderRepository.Object,
            transactionRepository.Object,
            inventoryRepository.Object,
            supplierRepository.Object,
            warehouseRepository.Object,
            productRepository.Object,
            unitOfWork.Object,
            currentUser.Object,
            new CreatePurchaseOrderRequestValidator());

    public static CreatePurchaseOrderRequest ValidCreate(
        Guid? companyId = null,
        Guid? supplierId = null,
        Guid? warehouseId = null,
        string orderNumber = "PO-001",
        Guid? productId = null,
        int quantity = 10,
        decimal unitPrice = 25m) =>
        new()
        {
            CompanyId = companyId,
            SupplierId = supplierId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            OrderNumber = orderNumber,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(5),
            Items =
            [
                new CreatePurchaseOrderItemRequest
                {
                    ProductId = productId ?? Guid.NewGuid(),
                    Quantity = quantity,
                    UnitPrice = unitPrice
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

    public static Supplier CreateSupplier(Guid id, Guid companyId, string companyName = "Tedarikçi A") =>
        new()
        {
            Id = id,
            CompanyId = companyId,
            CompanyName = companyName,
            ContactName = "Ali",
            Phone = "555",
            Email = "a@test.com",
            Address = "İstanbul",
            TaxNumber = "123",
            CreatedAt = DateTime.UtcNow
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

    public static PurchaseOrder CreateEntity(
        Guid? id = null,
        Guid? companyId = null,
        Guid? supplierId = null,
        Guid? warehouseId = null,
        Guid? userId = null,
        PurchaseOrderStatus status = PurchaseOrderStatus.Pending,
        string orderNumber = "PO-001",
        Guid? productId = null,
        int quantity = 10,
        decimal unitPrice = 25m,
        int receivedQuantity = 0) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId ?? Guid.NewGuid(),
            SupplierId = supplierId ?? Guid.NewGuid(),
            WarehouseId = warehouseId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            OrderNumber = orderNumber,
            TotalAmount = quantity * unitPrice,
            Status = status,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow,
            Items =
            [
                new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId ?? Guid.NewGuid(),
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    ReceivedQuantity = receivedQuantity
                }
            ]
        };

    public static void SetupSameCompanyReferences(
        Mock<ISupplierRepository> supplierRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IProductRepository> productRepository,
        Guid companyId,
        Guid supplierId,
        Guid warehouseId,
        Guid productId)
    {
        supplierRepository
            .Setup(r => r.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSupplier(supplierId, companyId));
        warehouseRepository
            .Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWarehouse(warehouseId, companyId));
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
