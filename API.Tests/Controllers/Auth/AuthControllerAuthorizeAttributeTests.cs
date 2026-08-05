using System.Reflection;
using API.Controllers;
using Core.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;

namespace API.Tests.Controllers.Auth;

/// <summary>
/// AuthController endpoint yetkilendirme attribute'ları (reflection).
/// Controller birim testleri filtre pipeline'ını çalıştırmaz; bu testler attribute sözleşmesini doğrular.
/// </summary>
public class AuthControllerAuthorizeAttributeTests
{
    /// <summary>Login: [AllowAnonymous] — kimlik doğrulama gerekmez.</summary>
    [Fact]
    public void Login_AllowAnonymousAttributeVar()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Login));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().ContainSingle();
    }

    /// <summary>Register: [Authorize(Roles = CompanyAdmins)] — SuperAdmin + CompanyAdmin.</summary>
    [Fact]
    public void Register_AuthorizeRolesCompanyAdmins()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Register));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin}");
    }

    /// <summary>Me: [Authorize] — kimlik doğrulama zorunlu, rol kısıtı yok.</summary>
    [Fact]
    public void Me_AuthorizeAttributeVar_RolesBos()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Me));
        method.Should().NotBeNull();

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().BeNullOrEmpty();
        method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Should().BeEmpty();
    }
}
