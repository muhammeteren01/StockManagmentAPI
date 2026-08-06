using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.Companies;

/// <summary>
/// CompaniesController yetkilendirme attribute'ları (reflection).
/// Controller birim testleri Authorize filtresini çalıştırmaz; attribute sözleşmesini doğrular.
/// Claims yok / yanlış rol → gerçek 401/403 için integration (pipeline) gerekir.
/// </summary>
public class CompaniesControllerAuthorizeAttributeTests
{
    /// <summary>Controller sınıfı: [Authorize(Roles = SuperAdminOnly)].</summary>
    [Fact]
    public void Controller_AuthorizeRolesSuperAdminOnly()
    {
        var authorize = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.SuperAdminOnly);
        authorize.Roles.Should().Be(AppRoles.SuperAdmin);
    }

    /// <summary>
    /// Create: AllowAnonymous yok; sınıf seviyesindeki SuperAdminOnly geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Create_WhenNoAllowAnonymous_RequiresAuthenticationViaControllerAuthorize()
    {
        var method = typeof(CompaniesController).GetMethod(nameof(CompaniesController.Create));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Create anonim çağrıya açık olmamalı");

        var authorize = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.SuperAdminOnly);
    }

    /// <summary>
    /// Create: yalnızca SuperAdmin rolü Authorize.Roles içinde.
    /// Senaryo 2 — claims var ama SuperAdmin değil → Roles listesinde olmadığı için filtre reddeder (403).
    /// Not: Metodu doğrudan çağırmak filtreyi atlar; bu sözleşme testidir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.CompanyAdmin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public void Create_WhenRoleIsNotSuperAdmin_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var authorize = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.SuperAdminOnly);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().NotContain(role.ToString());
    }

    /// <summary>Tüm public action'lar sınıf seviyesindeki Authorize'ı miras alır; AllowAnonymous yok.</summary>
    [Theory]
    [InlineData(nameof(CompaniesController.GetAll))]
    [InlineData(nameof(CompaniesController.GetById))]
    [InlineData(nameof(CompaniesController.Create))]
    [InlineData(nameof(CompaniesController.Update))]
    [InlineData(nameof(CompaniesController.Delete))]
    public void Actions_HaveNoAllowAnonymous(string methodName)
    {
        var method = typeof(CompaniesController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
    }
}
