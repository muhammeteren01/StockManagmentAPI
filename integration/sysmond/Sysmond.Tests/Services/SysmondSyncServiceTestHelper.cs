using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using Integration.Sysmond.Core.DTOs.Company;
using Integration.Sysmond.Core.DTOs.Despatches;
using Integration.Sysmond.Core.DTOs.Inventory;
using Integration.Sysmond.Core.DTOs.Stocks;
using Integration.Sysmond.Core.DTOs.Warehouses;
using Integration.Sysmond.Core.Mappings;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService birim testleri için ortak kurulum.</summary>
internal static class SysmondSyncServiceTestHelper
{
    public static SysmondSyncService CreateSut(
        Mock<ISysmondStockQueryService> stockQuery,
        Mock<ISysmondInventoryQueryService> inventoryQuery,
        Mock<IProductRepository> productRepository,
        Mock<ICompanyRepository> companyRepository,
        Mock<IWarehouseRepository> warehouseRepository,
        Mock<IInventoryRepository> inventoryRepository,
        Mock<IUnitOfWork> unitOfWork,
        Mock<ISysmondDespatchQueryService>? despatchQuery = null,
        Mock<IUserRepository>? userRepository = null,
        Mock<IPurchaseOrderRepository>? purchaseOrderRepository = null,
        Mock<ISysmondDespatchCommandService>? despatchCommand = null,
        Mock<ISysmondActQueryService>? actQuery = null,
        Mock<IActRepository>? actRepository = null,
        Mock<IActAddressRepository>? actAddressRepository = null,
        Mock<ISysmondStockCommandService>? stockCommand = null,
        Mock<ISysmondStockReceiptQueryService>? stockReceiptQuery = null,
        Mock<IStockTransactionRepository>? stockTransactionRepository = null) =>
        new(
            stockQuery.Object,
            inventoryQuery.Object,
            (despatchQuery ?? new Mock<ISysmondDespatchQueryService>()).Object,
            (actQuery ?? new Mock<ISysmondActQueryService>()).Object,
            (stockReceiptQuery ?? new Mock<ISysmondStockReceiptQueryService>()).Object,
            (stockCommand ?? new Mock<ISysmondStockCommandService>()).Object,
            (despatchCommand ?? new Mock<ISysmondDespatchCommandService>()).Object,
            productRepository.Object,
            companyRepository.Object,
            warehouseRepository.Object,
            inventoryRepository.Object,
            (purchaseOrderRepository ?? new Mock<IPurchaseOrderRepository>()).Object,
            (actRepository ?? new Mock<IActRepository>()).Object,
            (actAddressRepository ?? new Mock<IActAddressRepository>()).Object,
            (stockTransactionRepository ?? new Mock<IStockTransactionRepository>()).Object,
            (userRepository ?? new Mock<IUserRepository>()).Object,
            unitOfWork.Object,
            NullLogger<SysmondSyncService>.Instance);

    public static Company CreateCompany(Guid id, string name = "Alıcı") =>
        new()
        {
            Id = id,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static User CreateUser(Guid companyId, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            Email = "sync@test.local",
            PasswordHash = "x",
            FirstName = "Sync",
            LastName = "User",
            Role = UserRole.CompanyAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

    public static Product CreateProduct(
        Guid companyId,
        Guid? externalSysmondId = null,
        Guid? id = null,
        string sku = "SKU-1") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            ExternalSysmondId = externalSysmondId ?? Guid.NewGuid(),
            Sku = sku,
            Name = "Ürün",
            UnitPrice = 10,
            SellingPrice = 12,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

    public static Warehouse CreateWarehouse(
        Guid companyId,
        Guid? externalSysmondId = null,
        Guid? id = null,
        string name = "Depo") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            ExternalSysmondId = externalSysmondId ?? Guid.NewGuid(),
            Name = name,
            IsActive = true
        };

    public static SysmondCompanyPeriodDto CreatePeriod(Guid companyId, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CompanyId = companyId,
            Name = "2026",
            IsActive = true
        };

    public static SysmondStockDto CreateStock(
        Guid companyId,
        Guid? id = null,
        string code = "SKU-1",
        string name = "Ürün 1") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            Code = code,
            Name = name,
            Type = 10,
            IsActive = true,
            Prices = Array.Empty<SysmondStockPriceDto>(),
            OpeningQuantity = Array.Empty<SysmondOpeningQuantityDto>()
        };

    public static SysmondWarehouseDto CreateWarehouseDto(
        Guid companyId,
        Guid? id = null,
        string name = "Depo") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            IsActive = true
        };

    public static SysmondStockBalanceDto CreateBalance(
        Guid stockId,
        Guid warehouseId,
        double rem) =>
        new()
        {
            StockId = stockId,
            WarehouseId = warehouseId,
            Rem = rem,
            QuantityIn = rem,
            QuantityOut = 0
        };

    public static SysmondDespatchDto CreateDespatch(
        Guid? id = null,
        int direction = SysmondDespatchMapper.DirectionIncoming,
        string? docNo = "IRS-1",
        Guid? companyPeriodId = null,
        Guid? companyAddressId = null,
        Guid? deliveryAddressId = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Direction = direction,
            Status = 21,
            DocNo = docNo,
            CompanyPeriodId = companyPeriodId ?? Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            IssueDate = DateTime.UtcNow.Date,
            ActName = "Test Cari",
            ActVknTckn = "1234567890",
            CompanyAddressId = companyAddressId,
            DeliveryAddressId = deliveryAddressId
        };

    public static SysmondDespatchItemDto CreateDespatchItem(
        Guid despatchId,
        Guid stockId,
        Guid warehouseId,
        Guid? id = null,
        double quantity = 5) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            DespatchId = despatchId,
            StockId = stockId,
            WarehouseId = warehouseId,
            Quantity = quantity,
            Name = "Kalem",
            UnitPrice = 10
        };
}
