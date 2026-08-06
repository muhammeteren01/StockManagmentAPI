using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.Inventories;

/// <summary>
/// InventoriesController yetkilendirme attribute'ları (reflection).
/// Controller birim testleri Authorize filtresini çalıştırmaz; attribute sözleşmesini doğrular.
/// Claims yok / yanlış rol → gerçek 401/403 için integration (pipeline) gerekir.
/// </summary>
public class InventoriesControllerAuthorizeAttributeTests
{
    /// <summary>Controller sınıfı: [Authorize(Roles = AppRoles.All)].</summary>
    [Fact]
    public void Controller_AuthorizeRolesAll()
    {
        var authorize = typeof(InventoriesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.All);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin},{AppRoles.Manager},{AppRoles.Staff}");

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.Manager);
        allowed.Should().Contain(AppRoles.Staff);
    }

    /// <summary>
    /// Okuma action'ları: AllowAnonymous yok; sınıf seviyesindeki AppRoles.All geçerli.
    /// Senaryo 1 — claims yok / anonim: pipeline kimlik ister (401/403).
    /// </summary>
    [Theory]
    [InlineData(nameof(InventoriesController.GetAll))]
    [InlineData(nameof(InventoriesController.GetById))]
    [InlineData(nameof(InventoriesController.GetByProductAndWarehouse))]
    [InlineData(nameof(InventoriesController.GetByWarehouse))]
    [InlineData(nameof(InventoriesController.GetByProduct))]
    public void ReadActions_WhenNoAllowAnonymous_RequireAuthenticationViaControllerAuthorize(string methodName)
    {
        var method = typeof(InventoriesController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty($"{methodName} anonim çağrıya açık olmamalı");

        // Metot seviyesinde ayrı Authorize yok; sınıf AppRoles.All miras alınır.
        method.GetCustomAttribute<AuthorizeAttribute>(inherit: false).Should().BeNull();

        var authorize = typeof(InventoriesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.All);
    }

    /// <summary>
    /// Sınıf Roles: tüm roller; bilinen olmayan rol listede yok.
    /// Senaryo 2 — yanlış rol → filtre reddeder (403). Inventory'de yazma yok.
    /// </summary>
    [Theory]
    [InlineData("Guest")]
    public void Controller_WhenRoleIsNotInAll_RoleIsExcludedFromAuthorizeRoles(string role)
    {
        var authorize = typeof(InventoriesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.All);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().Contain(AppRoles.Manager);
        allowed.Should().Contain(AppRoles.Staff);
        allowed.Should().NotContain(role);
        Enum.GetNames<UserRole>().Should().NotContain(role);
    }

    /// <summary>Tüm public action'larda AllowAnonymous yok.</summary>
    [Theory]
    [InlineData(nameof(InventoriesController.GetAll))]
    [InlineData(nameof(InventoriesController.GetById))]
    [InlineData(nameof(InventoriesController.GetByProductAndWarehouse))]
    [InlineData(nameof(InventoriesController.GetByWarehouse))]
    [InlineData(nameof(InventoriesController.GetByProduct))]
    public void Actions_HaveNoAllowAnonymous(string methodName)
    {
        var method = typeof(InventoriesController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
    }
}
