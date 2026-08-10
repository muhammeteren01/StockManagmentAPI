using System.Reflection;
using API.Controllers;
using Core.DTOs.Sysmond;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Sysmond;

/// <summary>SysmondController sync endpoint birim testleri.</summary>
public class SysmondControllerSyncProductsTests
{
    private readonly Mock<ISysmondSyncService> _syncService = new();
    private readonly SysmondController _sut;

    public SysmondControllerSyncProductsTests()
    {
        _sut = new SysmondController(_syncService.Object);
    }

    /// <summary>Controller: yerel JWT / SuperAdmin yok; [AllowAnonymous].</summary>
    [Fact]
    public void Controller_HasAllowAnonymous_NotSuperAdminAuthorize()
    {
        typeof(SysmondController).GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().ContainSingle();

        var authorize = typeof(SysmondController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().BeNull();
    }

    /// <summary>SyncProducts: Bearer + companyId → Ok(200); SyncProductsAsync token ile çağrılır.</summary>
    [Fact]
    public async Task SyncProducts_WhenBearerAndCompanyIdProvided_ReturnsOkWithResult()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        const string token = "sysmond-access-token";
        var expected = new SysmondProductSyncResult { Fetched = 2, Created = 1, Updated = 1 };
        _syncService
            .Setup(s => s.SyncProductsAsync(companyId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        SetAuthorizationHeader($"Bearer {token}");

        var result = await _sut.SyncProducts(companyId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _syncService.Verify(
            s => s.SyncProductsAsync(companyId, token, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>SyncProducts: companyId boş Guid → ValidationException; servis çağrılmaz.</summary>
    [Fact]
    public async Task SyncProducts_WhenCompanyIdEmpty_ThrowsValidationException()
    {
        SetAuthorizationHeader("Bearer some-token");

        var act = async () => await _sut.SyncProducts(Guid.Empty, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("companyId");
        _syncService.Verify(
            s => s.SyncProductsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>SyncProducts: Authorization Bearer yok → ValidationException; servis çağrılmaz.</summary>
    [Fact]
    public async Task SyncProducts_WhenBearerMissing_ThrowsValidationException()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        SetAuthorizationHeader(null);

        var act = async () => await _sut.SyncProducts(companyId, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey("Authorization");
        _syncService.Verify(
            s => s.SyncProductsAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>SyncInventories: Bearer + companyId → Ok.</summary>
    [Fact]
    public async Task SyncInventories_WhenBearerAndCompanyIdProvided_ReturnsOkWithResult()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        const string token = "sysmond-access-token";
        var expected = new SysmondInventorySyncResult { Fetched = 3, Created = 2, Updated = 1 };
        _syncService
            .Setup(s => s.SyncInventoriesAsync(companyId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        SetAuthorizationHeader($"Bearer {token}");

        var result = await _sut.SyncInventories(companyId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _syncService.Verify(
            s => s.SyncInventoriesAsync(companyId, token, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>SyncAll: products + inventories birleşik sonuç.</summary>
    [Fact]
    public async Task SyncAll_WhenBearerAndCompanyIdProvided_ReturnsOkWithFullResult()
    {
        var companyId = Guid.Parse("f9e4c15a-307a-d6e5-495a-3a22008d01a1");
        const string token = "sysmond-access-token";
        var expected = new SysmondFullSyncResult
        {
            Products = new SysmondProductSyncResult { Created = 1 },
            Inventories = new SysmondInventorySyncResult { Created = 2 }
        };
        _syncService
            .Setup(s => s.SyncAllAsync(companyId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        SetAuthorizationHeader($"Bearer {token}");

        var result = await _sut.SyncAll(companyId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _syncService.Verify(
            s => s.SyncAllAsync(companyId, token, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetAuthorizationHeader(string? value)
    {
        var httpContext = new DefaultHttpContext();
        if (value is not null)
            httpContext.Request.Headers.Authorization = value;
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }
}
