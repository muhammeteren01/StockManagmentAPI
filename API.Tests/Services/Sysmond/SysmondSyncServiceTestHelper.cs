using Core.DTOs.Sysmond;
using Core.Entities;
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
        Mock<IUnitOfWork> unitOfWork) =>
        new(
            stockQuery.Object,
            inventoryQuery.Object,
            productRepository.Object,
            companyRepository.Object,
            warehouseRepository.Object,
            inventoryRepository.Object,
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
        string name = "Ana Depo",
        string? code = "WH-1") =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            WarehouseCode = code,
            IsActive = true
        };

    public static SysmondStockBalanceDto CreateBalance(
        Guid stockId,
        Guid warehouseId,
        double rem = 10) =>
        new()
        {
            StockId = stockId,
            WarehouseId = warehouseId,
            Rem = rem,
            QuantityIn = rem,
            QuantityOut = 0
        };
}
