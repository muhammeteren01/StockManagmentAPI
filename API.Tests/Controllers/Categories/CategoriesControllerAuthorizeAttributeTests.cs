using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.Categories;

/// <summary>
/// CategoriesController yetkilendirme attribute'ları (reflection).
/// Controller birim testleri Authorize filtresini çalıştırmaz; attribute sözleşmesini doğrular.
/// Claims yok / yanlış rol → gerçek 401/403 için integration (pipeline) gerekir.
/// </summary>
public class CategoriesControllerAuthorizeAttributeTests
{
    /// <summary>Controller sınıfı: [Authorize] (rol kısıtı yok; kimlik zorunlu).</summary>
    [Fact]
    public void Controller_AuthorizeAuthenticatedOnly()
    {
        var authorize = typeof(CategoriesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().BeNullOrEmpty();
    }

    /// <summary>Okuma action'ları: AppRoles.All (Manager ve Staff dahil).</summary>
    [Theory]
    [InlineData(nameof(CategoriesController.GetAll))]
    [InlineData(nameof(CategoriesController.GetById))]
    [InlineData(nameof(CategoriesController.GetByCompany))]
    public void ReadActions_AuthorizeRolesAll(string methodName)
    {
        var method = typeof(CategoriesController).GetMethod(methodName);
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
    /// Create: AllowAnonymous yok; metot seviyesinde Writers Roles.
    /// Senaryo 1 — claims yok / anonim: pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Create_WhenNoAllowAnonymous_RequiresAuthenticationViaAuthorize()
    {
        var method = typeof(CategoriesController).GetMethod(nameof(CategoriesController.Create));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Create anonim çağrıya açık olmamalı");

        var authorize = method.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.Writers);
    }

    /// <summary>
    /// Create: yalnızca Writers; Staff Roles içinde yok.
    /// Senaryo 2 — claims var ama Writers değil → filtre reddeder (403).
    /// </summary>
    [Theory]
    [InlineData(UserRole.Staff)]
    public void Create_WhenRoleIsNotWriter_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var method = typeof(CategoriesController).GetMethod(nameof(CategoriesController.Create));
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

    /// <summary>Update: Writers; Staff hariç.</summary>
    [Fact]
    public void Update_AuthorizeRolesWriters()
    {
        var method = typeof(CategoriesController).GetMethod(nameof(CategoriesController.Update));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: false);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.Writers);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin},{AppRoles.Manager}");
    }

    /// <summary>Tüm public action'larda AllowAnonymous yok.</summary>
    [Theory]
    [InlineData(nameof(CategoriesController.GetAll))]
    [InlineData(nameof(CategoriesController.GetById))]
    [InlineData(nameof(CategoriesController.GetByCompany))]
    [InlineData(nameof(CategoriesController.Create))]
    [InlineData(nameof(CategoriesController.Update))]
    [InlineData(nameof(CategoriesController.Delete))]
    public void Actions_HaveNoAllowAnonymous(string methodName)
    {
        var method = typeof(CategoriesController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
    }
}
