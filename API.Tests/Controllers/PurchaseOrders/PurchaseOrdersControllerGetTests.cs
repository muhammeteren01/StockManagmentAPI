using API.Controllers;
using Core.DTOs.PurchaseOrders;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.PurchaseOrders;

/// <summary>PurchaseOrdersController okuma endpoint'leri birim testleri.</summary>
public class PurchaseOrdersControllerGetTests
{
    private readonly Mock<IPurchaseOrderService> _purchaseOrderService = new();
    private readonly PurchaseOrdersController _sut;

    public PurchaseOrdersControllerGetTests()
    {
        _sut = PurchaseOrdersControllerTestHelper.CreateSut(_purchaseOrderService);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<PurchaseOrderResponse>
        {
            PurchaseOrdersTestHelper.CreateOrderResponse(orderNumber: "PO-A"),
            PurchaseOrdersTestHelper.CreateOrderResponse(orderNumber: "PO-B")
        };
        _purchaseOrderService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _purchaseOrderService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetAll: boş liste Ok(200) ve boş dizi.</summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _purchaseOrderService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PurchaseOrderResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<PurchaseOrderResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>GetById: sipariş bulunduğunda Ok(200) ve PurchaseOrderResponse döner.</summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithOrderResponse()
    {
        var id = Guid.NewGuid();
        var expected = PurchaseOrdersTestHelper.CreateOrderResponse(id);
        _purchaseOrderService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _purchaseOrderService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetById: servis null → NotFound(404).</summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _purchaseOrderService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompany: servis liste döndüğünde Ok(200).</summary>
    [Fact]
    public async Task GetByCompany_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var companyId = Guid.NewGuid();
        var expected = new List<PurchaseOrderResponse>
        {
            PurchaseOrdersTestHelper.CreateOrderResponse(companyId: companyId)
        };
        _purchaseOrderService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _purchaseOrderService.Verify(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompany: şirket erişimi yok → ForbiddenException iletilir.</summary>
    [Fact]
    public async Task GetByCompany_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _purchaseOrderService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.GetByCompany(companyId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }
}
