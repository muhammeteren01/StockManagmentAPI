using Core.DTOs.Acts;
using Core.DTOs.Products;
using Core.DTOs.Warehouses;
using Core.Entities;
using Core.Enums;
using Core.Repositories;
using FluentAssertions;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;
using Integration.Sysmond.Service.Orchestration;
using Moq;

namespace Integration.Sysmond.Tests.Orchestration;

/// <summary>Product / Warehouse / Act orchestrator birim testleri (token + sync delegasyonu).</summary>
public class SysmondOrchestratorTests
{
    private static readonly Guid CompanyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
    private const string Token = "sysmond-access-token";

    [Fact]
    public async Task ProductOrchestrator_Create_GetsTokenAndCallsCreateStock()
    {
        var tokenProvider = new Mock<ISysmondAccessTokenProvider>();
        var sync = new Mock<ISysmondSyncService>();
        var products = new Mock<IProductRepository>();
        tokenProvider.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Token);

        var measureUnitId = Guid.NewGuid();
        var request = new CreateProductRequest
        {
            CompanyId = CompanyId,
            CategoryId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Sku = "SKU-1",
            Name = "Laptop",
            UnitPrice = 100m,
            SellingPrice = 120m,
            MeasureUnitId = measureUnitId,
            Status = ProductStatus.Active
        };
        var expected = new ProductResponse { Id = Guid.NewGuid(), Name = "Laptop", Sku = "SKU-1" };
        sync.Setup(s => s.CreateStockAsync(
                CompanyId,
                Token,
                It.Is<SysmondCreateStockRequest>(r => r.Code == "SKU-1" && r.MeasureUnitId == measureUnitId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new SysmondProductOrchestrator(tokenProvider.Object, sync.Object, products.Object);
        var result = await sut.CreateAsync(CompanyId, request, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        tokenProvider.Verify(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        sync.VerifyAll();
    }

    [Fact]
    public async Task ProductOrchestrator_Create_WhenMeasureUnitMissing_Throws()
    {
        var sut = new SysmondProductOrchestrator(
            new Mock<ISysmondAccessTokenProvider>().Object,
            new Mock<ISysmondSyncService>().Object,
            new Mock<IProductRepository>().Object);

        var request = new CreateProductRequest
        {
            CompanyId = CompanyId,
            CategoryId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Sku = "SKU-1",
            Name = "Laptop",
            UnitPrice = 100m,
            SellingPrice = 120m
        };

        var act = async () => await sut.CreateAsync(CompanyId, request, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*MeasureUnitId*");
    }

    [Fact]
    public async Task WarehouseOrchestrator_Create_GetsTokenAndCallsCreateWarehouse()
    {
        var tokenProvider = new Mock<ISysmondAccessTokenProvider>();
        var sync = new Mock<ISysmondSyncService>();
        var warehouses = new Mock<IWarehouseRepository>();
        tokenProvider.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Token);

        var request = new CreateWarehouseRequest
        {
            CompanyId = CompanyId,
            Name = "Merkez",
            Location = "IST-01",
            Capacity = 100
        };
        var expected = new WarehouseResponse { Id = Guid.NewGuid(), Name = "Merkez", Location = "IST-01" };
        sync.Setup(s => s.CreateWarehouseAsync(
                CompanyId,
                Token,
                It.Is<SysmondCreateWarehouseRequest>(r => r.Name == "Merkez" && r.WarehouseCode == "IST-01"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new SysmondWarehouseOrchestrator(tokenProvider.Object, sync.Object, warehouses.Object);
        var result = await sut.CreateAsync(CompanyId, request, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        tokenProvider.Verify(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActOrchestrator_Create_MapsDomainResponse()
    {
        var tokenProvider = new Mock<ISysmondAccessTokenProvider>();
        var sync = new Mock<ISysmondSyncService>();
        var acts = new Mock<IActRepository>();
        tokenProvider.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Token);

        var request = new CreateActRequest { Name = "Test Cari", Type = 20 };
        var remote = new SysmondActResponse
        {
            Id = Guid.NewGuid(),
            ExternalSysmondId = Guid.NewGuid(),
            Name = "Test Cari",
            Type = 20
        };
        sync.Setup(s => s.CreateActAsync(
                CompanyId,
                Token,
                It.Is<SysmondCreateActRequest>(r => r.Name == "Test Cari" && r.Type == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(remote);

        var sut = new SysmondActOrchestrator(tokenProvider.Object, sync.Object, acts.Object);
        var result = await sut.CreateAsync(CompanyId, request, CancellationToken.None);

        result.Name.Should().Be("Test Cari");
        result.CompanyId.Should().Be(CompanyId);
        result.ExternalSysmondId.Should().Be(remote.ExternalSysmondId);
        result.Id.Should().Be(remote.Id);
    }

    [Fact]
    public async Task WarehouseOrchestrator_Update_UsesExternalSysmondId()
    {
        var tokenProvider = new Mock<ISysmondAccessTokenProvider>();
        var sync = new Mock<ISysmondSyncService>();
        var warehouses = new Mock<IWarehouseRepository>();
        tokenProvider.Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Token);

        var warehouseId = Guid.NewGuid();
        var externalId = Guid.NewGuid();
        warehouses.Setup(r => r.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Warehouse
            {
                Id = warehouseId,
                CompanyId = CompanyId,
                Name = "Eski",
                ExternalSysmondId = externalId
            });

        var request = new UpdateWarehouseRequest { Name = "Yeni", Location = "ANK", IsActive = true };
        var expected = new WarehouseResponse { Id = warehouseId, Name = "Yeni" };
        sync.Setup(s => s.UpdateWarehouseAsync(
                CompanyId,
                Token,
                externalId,
                It.Is<SysmondUpdateWarehouseRequest>(r => r.Name == "Yeni"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = new SysmondWarehouseOrchestrator(tokenProvider.Object, sync.Object, warehouses.Object);
        var result = await sut.UpdateAsync(warehouseId, request, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }
}
