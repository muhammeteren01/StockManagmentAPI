using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Service.Services.Sysmond;

namespace API.Tests.Services.Sysmond;

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
        Mock<IPurchaseOrderRepository>? purchaseOrderRepository = null) =>
        new(
            stockQuery.Object,
            inventoryQuery.Object,
            (despatchQuery ?? new Mock<ISysmondDespatchQueryService>()).Object,
            Mock.Of<ISysmondStockCommandService>(),
            productRepository.Object,
            companyRepository.Object,
            warehouseRepository.Object,
            inventoryRepository.Object,
            (purchaseOrderRepository ?? new Mock<IPurchaseOrderRepository>()).Object,
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
            IsActive = true,
            CreatedAt = DateTime.UtcNow
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
