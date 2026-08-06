using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Warehouses;

/// <summary>WarehousesController.Delete birim testleri.</summary>
public class WarehousesControllerDeleteTests
{
    private readonly Mock<IWarehouseService> _warehouseService = new();
    private readonly WarehousesController _sut;

    public WarehousesControllerDeleteTests()
    {
        _sut = new WarehousesController(_warehouseService.Object);
    }

    /// <summary>
    /// Delete: AllowAnonymous yok; metot seviyesinde Writers Roles geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Delete_WhenNoAllowAnonymous_RequiresAuthenticationViaAuthorize()
    {
        var method = typeof(WarehousesController).GetMethod(nameof(WarehousesController.Delete));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Delete anonim çağrıya açık olmamalı");

        var authorize = method.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.Writers);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin},{AppRoles.Manager}");
    }

    /// <summary>
    /// Delete: yalnızca Writers (SuperAdmin / CompanyAdmin / Manager); Staff Roles içinde yok.
    /// Senaryo 2 — claims var ama Writers değil → filtre reddeder (403).
    /// Not: Metodu doğrudan çağırmak filtreyi atlar; bu sözleşme testidir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Staff)]
    public void Delete_WhenRoleIsNotWriter_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var method = typeof(WarehousesController).GetMethod(nameof(WarehousesController.Delete));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.Writers);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().Contain(AppRoles.Manager);
        allowed.Should().NotContain(role.ToString());
    }

    /// <summary>
    /// Delete: servis başarılı → NoContent(204);
    /// DeleteAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _warehouseService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _warehouseService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Delete: depo yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Delete_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _warehouseService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Warehouse bulunamadı: {id}"));

        var act = async () => await _sut.Delete(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Warehouse bulunamadı: {id}");
    }
}
