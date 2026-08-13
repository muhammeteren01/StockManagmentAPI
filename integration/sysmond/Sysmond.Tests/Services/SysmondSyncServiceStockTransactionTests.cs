using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Core.UnitOfWork;
using FluentAssertions;
using Integration.Sysmond.Core.DTOs.Company;
using Integration.Sysmond.Core.DTOs.StockReceipts;
using Integration.Sysmond.Core.Mappings;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Service.Services;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>Sysmond stock-receipt → StockTransaction sync testleri.</summary>
public class SysmondSyncServiceStockTransactionTests
{
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private static readonly Guid PeriodId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private const string AccessToken = "token";

    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondStockReceiptQueryService> _receiptQuery = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IStockTransactionRepository> _txRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly SysmondSyncService _sut;

    public SysmondSyncServiceStockTransactionTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork,
            userRepository: _userRepository,
            stockReceiptQuery: _receiptQuery,
            stockTransactionRepository: _txRepository);

        _companyRepository.Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));
        _userRepository.Setup(r => r.GetByCompanyIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([SysmondSyncServiceTestHelper.CreateUser(CompanyId)]);
        _inventoryQuery.Setup(s => s.GetMyCompanyPeriodsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([SysmondSyncServiceTestHelper.CreatePeriod(CompanyId, PeriodId)]);
        _txRepository.Setup(r => r.GetSysmondByCompanyPeriodAsync(CompanyId, PeriodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<StockTransaction>());
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task SyncStockTransactions_WhenEntryReceipt_CreatesInTransaction()
    {
        var stockExt = Guid.NewGuid();
        var whExt = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var product = SysmondSyncServiceTestHelper.CreateProduct(CompanyId, stockExt);
        var warehouse = SysmondSyncServiceTestHelper.CreateWarehouse(CompanyId, whExt);

        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Entry, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysmondStockReceiptDto
                {
                    Id = receiptId,
                    CompanyPeriodId = PeriodId,
                    Type = SysmondStockReceiptTypes.Entry,
                    DocNo = "ENT-1",
                    WarehouseId = whExt,
                    TransactionDate = DateTime.UtcNow.Date,
                    IsDraft = false
                }
            ]);
        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Exit, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockReceiptDto>());
        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Transfer, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockReceiptDto>());

        _receiptQuery.Setup(s => s.GetStockReceiptItemsAsync(AccessToken, receiptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysmondStockReceiptItemDto
                {
                    Id = itemId,
                    StockReceiptId = receiptId,
                    StockId = stockExt,
                    WarehouseId = whExt,
                    Quantity = 5
                }
            ]);

        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(whExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warehouse);
        _txRepository.Setup(r => r.GetByExternalSysmondIdAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransaction?)null);

        StockTransaction? added = null;
        _txRepository.Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncStockTransactionsAsync(CompanyId, AccessToken);

        result.Created.Should().Be(1);
        added.Should().NotBeNull();
        added!.TransactionType.Should().Be(TransactionType.In);
        added.Quantity.Should().Be(5);
        added.ExternalSysmondId.Should().Be(itemId);
        added.ExternalSysmondDespatchId.Should().Be(receiptId);
        added.ReferenceNo.Should().Be("ENT-1");
        added.Notes.Should().Contain("Giriş");
    }

    [Fact]
    public async Task SyncStockTransactions_WhenTransfer_CreatesOutAndIn()
    {
        var stockExt = Guid.NewGuid();
        var sourceExt = Guid.NewGuid();
        var targetExt = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var product = SysmondSyncServiceTestHelper.CreateProduct(CompanyId, stockExt);
        var sourceWh = SysmondSyncServiceTestHelper.CreateWarehouse(CompanyId, sourceExt, name: "Kaynak");
        var targetWh = SysmondSyncServiceTestHelper.CreateWarehouse(CompanyId, targetExt, name: "Hedef");

        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Entry, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockReceiptDto>());
        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Exit, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SysmondStockReceiptDto>());
        _receiptQuery.Setup(s => s.GetStockReceiptsAsync(
                AccessToken, PeriodId, SysmondStockReceiptTypes.Transfer, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysmondStockReceiptDto
                {
                    Id = receiptId,
                    CompanyPeriodId = PeriodId,
                    Type = SysmondStockReceiptTypes.Transfer,
                    WarehouseId = sourceExt,
                    TargetWarehouseId = targetExt,
                    IsDraft = false
                }
            ]);
        _receiptQuery.Setup(s => s.GetStockReceiptItemsAsync(AccessToken, receiptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysmondStockReceiptItemDto
                {
                    Id = itemId,
                    StockReceiptId = receiptId,
                    StockId = stockExt,
                    WarehouseId = sourceExt,
                    Quantity = 3
                }
            ]);

        _productRepository.Setup(r => r.GetByExternalSysmondIdAsync(stockExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(sourceExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceWh);
        _warehouseRepository.Setup(r => r.GetByExternalSysmondIdAsync(targetExt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetWh);
        _txRepository.Setup(r => r.GetByExternalSysmondIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTransaction?)null);

        var added = new List<StockTransaction>();
        _txRepository.Setup(r => r.AddAsync(It.IsAny<StockTransaction>(), It.IsAny<CancellationToken>()))
            .Callback<StockTransaction, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var result = await _sut.SyncStockTransactionsAsync(CompanyId, AccessToken);

        result.Created.Should().Be(2);
        added.Should().HaveCount(2);
        added.Should().Contain(x => x.TransactionType == TransactionType.TransferOut && x.WarehouseId == sourceWh.Id);
        added.Should().Contain(x =>
            x.TransactionType == TransactionType.TransferIn &&
            x.WarehouseId == targetWh.Id &&
            x.ExternalSysmondId == SysmondStockReceiptMapper.ToTransferInExternalId(itemId));
    }
}
