using API.Controllers;
using Core.DTOs.Suppliers;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Suppliers;

/// <summary>SuppliersController okuma endpoint'leri birim testleri.</summary>
public class SuppliersControllerGetTests
{
    private readonly Mock<ISupplierService> _supplierService = new();
    private readonly SuppliersController _sut;

    public SuppliersControllerGetTests()
    {
        _sut = new SuppliersController(_supplierService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<SupplierResponse>
        {
            SuppliersTestHelper.CreateSupplierResponse(companyName: "Alpha"),
            SuppliersTestHelper.CreateSupplierResponse(companyName: "Beta")
        };
        _supplierService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _supplierService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetAll: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _supplierService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SupplierResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SupplierResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetById: tedarikçi bulunduğunda Ok(200) ve SupplierResponse döner.
    /// </summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithSupplierResponse()
    {
        var id = Guid.NewGuid();
        var expected = SuppliersTestHelper.CreateSupplierResponse(id);
        _supplierService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _supplierService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetById: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _supplierService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _supplierService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var companyId = Guid.NewGuid();
        var expected = new List<SupplierResponse>
        {
            SuppliersTestHelper.CreateSupplierResponse(companyId: companyId)
        };
        _supplierService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _supplierService.Verify(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var companyId = Guid.NewGuid();
        _supplierService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SupplierResponse>());

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<SupplierResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetByCompany: tenant erişim yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenAccessDenied_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _supplierService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.GetByCompany(companyId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }
}
