using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.StockTransactions;

/// <summary>
/// StockTransactionsController yetkilendirme attribute'ları (reflection).
/// Controller birim testleri Authorize filtresini çalıştırmaz; attribute sözleşmesini doğrular.
/// Claims yok / yanlış rol → gerçek 401/403 için integration (pipeline) gerekir.
/// </summary>
public class StockTransactionsControllerAuthorizeAttributeTests
{
    /// <summary>Controller sınıfı: [Authorize] (rol kısıtı yok; kimlik zorunlu).</summary>
    [Fact]
    public void Controller_AuthorizeAuthenticatedOnly()
    {
        var authorize = typeof(StockTransactionsController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().BeNullOrEmpty();
    }

    /// <summary>Okuma action'ları: AppRoles.All (Manager ve Staff dahil).</summary>
    [Theory]
    [InlineData(nameof(StockTransactionsController.GetAll))]
    [InlineData(nameof(StockTransactionsController.GetById))]
    [InlineData(nameof(StockTransactionsController.GetByProduct))]
    [InlineData(nameof(StockTransactionsController.GetByWarehouse))]
    public void ReadActions_AuthorizeRolesAll(string methodName)
    {
        var method = typeof(StockTransactionsController).GetMethod(methodName);
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.All);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin},{AppRoles.Manager},{AppRoles.Staff}");

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.Manager);
        allowed.Should().Contain(AppRoles.Staff);
    }

    /// <summary>
    /// Create: AllowAnonymous yok; metot seviyesinde StockOps Roles.
    /// Senaryo 1 — claims yok / anonim: pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Create_WhenNoAllowAnonymous_RequiresAuthenticationViaAuthorize()
    {
        var method = typeof(StockTransactionsController).GetMethod(nameof(StockTransactionsController.Create));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Create anonim çağrıya açık olmamalı");

        var authorize = method.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.StockOps);
        authorize.Roles.Should().Be(AppRoles.All);
    }

    /// <summary>
    /// Create: StockOps = All; Staff dahil (StockOps Writers değil).
    /// Senaryo 2 — bilinen rol listede; Guest dışarıda.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Staff)]
    [InlineData(UserRole.Manager)]
    public void Create_WhenRoleIsStockOps_RoleIsIncludedInAuthorizeRoles(UserRole role)
    {
        var method = typeof(StockTransactionsController).GetMethod(nameof(StockTransactionsController.Create));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.StockOps);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().Contain(AppRoles.Manager);
        allowed.Should().Contain(AppRoles.Staff);
        allowed.Should().Contain(role.ToString());
        allowed.Should().NotContain("Guest");
    }

    /// <summary>Tüm public action'larda AllowAnonymous yok.</summary>
    [Theory]
    [InlineData(nameof(StockTransactionsController.GetAll))]
    [InlineData(nameof(StockTransactionsController.GetById))]
    [InlineData(nameof(StockTransactionsController.GetByProduct))]
    [InlineData(nameof(StockTransactionsController.GetByWarehouse))]
    [InlineData(nameof(StockTransactionsController.Create))]
    public void Actions_HaveNoAllowAnonymous(string methodName)
    {
        var method = typeof(StockTransactionsController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
    }
}
