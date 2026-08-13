using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;

namespace Integration.Sysmond.Tests.Services;

/// <summary>SysmondSyncService UpdateIncoming/OutgoingDespatch birim testleri.</summary>
public class SysmondSyncServiceDespatchUpdateTests
{
    private readonly Mock<ISysmondStockQueryService> _stockQuery = new();
    private readonly Mock<ISysmondInventoryQueryService> _inventoryQuery = new();
    private readonly Mock<ISysmondDespatchCommandService> _despatchCommand = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<ICompanyRepository> _companyRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IPurchaseOrderRepository> _poRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Integration.Sysmond.Service.Services.SysmondSyncService _sut;

    private const string AccessToken = "sysmond-access-token";
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private static readonly Guid PeriodId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DespatchId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    public SysmondSyncServiceDespatchUpdateTests()
    {
        _sut = SysmondSyncServiceTestHelper.CreateSut(
            _stockQuery,
            _inventoryQuery,
            _productRepository,
            _companyRepository,
            _warehouseRepository,
            _inventoryRepository,
            _unitOfWork,
            purchaseOrderRepository: _poRepository,
            despatchCommand: _despatchCommand);

        _companyRepository
            .Setup(r => r.GetByIdAsync(CompanyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SysmondSyncServiceTestHelper.CreateCompany(CompanyId));
    }

    [Fact]
    public async Task UpdateIncomingDespatchAsync_WhenValid_UpdatesRemoteAndLocal()
    {
        var order = CreateLocalOrder(DespatchDirection.Incoming);
        _poRepository
            .Setup(r => r.GetByExternalSysmondIdWithItemsAsync(DespatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var request = new SysmondUpdateIncomingDespatchRequest
        {
            DocNo = "IRS-UPDATED",
            Description = "Güncellendi",
            IssueDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.UpdateIncomingDespatchAsync(
            CompanyId, AccessToken, DespatchId, request, CancellationToken.None);

        result.OrderNumber.Should().Be("IRS-UPDATED");
        order.OrderNumber.Should().Be("IRS-UPDATED");
        order.IssueDate.Should().Be(request.IssueDate);
        _despatchCommand.Verify(
            c => c.UpdateIncomingDraftAsync(
                AccessToken,
                It.Is<SysmondIncomingDespatchUpdateDto>(d =>
                    d.Id == DespatchId &&
                    d.CompanyPeriodId == PeriodId &&
                    d.DocNo == "IRS-UPDATED"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _poRepository.Verify(r => r.Update(order), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOutgoingDespatchAsync_WhenValid_UpdatesRemoteAndLocal()
    {
        var order = CreateLocalOrder(DespatchDirection.Outgoing);
        _poRepository
            .Setup(r => r.GetByExternalSysmondIdWithItemsAsync(DespatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var request = new SysmondUpdateOutgoingDespatchRequest
        {
            Description = "Outgoing update",
            ActualDespatchDate = new DateTime(2026, 8, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var result = await _sut.UpdateOutgoingDespatchAsync(
            CompanyId, AccessToken, DespatchId, request, CancellationToken.None);

        result.ActualDespatchDate.Should().Be(request.ActualDespatchDate);
        order.ActualDespatchDate.Should().Be(request.ActualDespatchDate);
        _despatchCommand.Verify(
            c => c.UpdateOutgoingDraftAsync(
                AccessToken,
                It.Is<SysmondOutgoingDespatchUpdateDto>(d =>
                    d.Id == DespatchId &&
                    d.CompanyPeriodId == PeriodId &&
                    d.Description == "Outgoing update"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateIncomingDespatchAsync_WhenWrongDirection_ThrowsInvalidOperationException()
    {
        var order = CreateLocalOrder(DespatchDirection.Outgoing);
        _poRepository
            .Setup(r => r.GetByExternalSysmondIdWithItemsAsync(DespatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.UpdateIncomingDespatchAsync(
            CompanyId,
            AccessToken,
            DespatchId,
            new SysmondUpdateIncomingDespatchRequest { DocNo = "X" },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*giden irsaliye*");
        _despatchCommand.Verify(
            c => c.UpdateIncomingDraftAsync(
                It.IsAny<string>(), It.IsAny<SysmondIncomingDespatchUpdateDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateIncomingDespatchAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        _poRepository
            .Setup(r => r.GetByExternalSysmondIdWithItemsAsync(DespatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrder?)null);

        var act = async () => await _sut.UpdateIncomingDespatchAsync(
            CompanyId,
            AccessToken,
            DespatchId,
            new SysmondUpdateIncomingDespatchRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateIncomingDespatchAsync_WhenCompanyPeriodMissing_ThrowsValidationException()
    {
        var order = CreateLocalOrder(DespatchDirection.Incoming);
        order.ExternalSysmondCompanyPeriodId = null;
        _poRepository
            .Setup(r => r.GetByExternalSysmondIdWithItemsAsync(DespatchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var act = async () => await _sut.UpdateIncomingDespatchAsync(
            CompanyId,
            AccessToken,
            DespatchId,
            new SysmondUpdateIncomingDespatchRequest { DocNo = "X" },
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("CompanyPeriodId");
    }

    private static PurchaseOrder CreateLocalOrder(DespatchDirection direction) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = CompanyId,
            DocumentType = direction == DespatchDirection.Incoming
                ? PurchaseOrderDocumentType.IncomingDespatch
                : PurchaseOrderDocumentType.OutgoingDespatch,
            Direction = direction,
            UserId = Guid.NewGuid(),
            OrderNumber = "IRS-OLD",
            Status = PurchaseOrderStatus.Saved,
            ExternalSysmondId = DespatchId,
            ExternalSysmondCompanyPeriodId = PeriodId,
            IssueDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            ActualDespatchDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow,
            Items = new List<PurchaseOrderItem>()
        };
}
