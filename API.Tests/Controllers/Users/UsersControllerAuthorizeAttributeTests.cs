using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.Users;

/// <summary>
/// UsersController yetkilendirme attribute'ları (reflection).
/// Controller birim testleri Authorize filtresini çalıştırmaz; attribute sözleşmesini doğrular.
/// Claims yok / yanlış rol → gerçek 401/403 için integration (pipeline) gerekir.
/// </summary>
public class UsersControllerAuthorizeAttributeTests
{
    /// <summary>Controller sınıfı: [Authorize(Roles = CompanyAdmins)].</summary>
    [Fact]
    public void Controller_AuthorizeRolesCompanyAdmins()
    {
        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin}");
    }

    /// <summary>
    /// Create: AllowAnonymous yok; sınıf seviyesindeki CompanyAdmins geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Create_WhenNoAllowAnonymous_RequiresAuthenticationViaControllerAuthorize()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.Create));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Create anonim çağrıya açık olmamalı");

        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);
    }

    /// <summary>
    /// Create: yalnızca SuperAdmin / CompanyAdmin; Manager / Staff Roles içinde yok.
    /// Senaryo 2 — claims var ama CompanyAdmins değil → filtre reddeder (403).
    /// Not: Metodu doğrudan çağırmak filtreyi atlar; bu sözleşme testidir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public void Create_WhenRoleIsNotCompanyAdmin_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().NotContain(role.ToString());
    }

    /// <summary>Tüm public action'lar sınıf seviyesindeki Authorize'ı miras alır; AllowAnonymous yok.</summary>
    [Theory]
    [InlineData(nameof(UsersController.GetAll))]
    [InlineData(nameof(UsersController.GetById))]
    [InlineData(nameof(UsersController.GetByCompany))]
    [InlineData(nameof(UsersController.GetByEmail))]
    [InlineData(nameof(UsersController.Create))]
    [InlineData(nameof(UsersController.Update))]
    [InlineData(nameof(UsersController.Delete))]
    public void Actions_HaveNoAllowAnonymous(string methodName)
    {
        var method = typeof(UsersController).GetMethod(methodName);
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty();
    }
}
