using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Users;

/// <summary>UsersController.Delete birim testleri.</summary>
public class UsersControllerDeleteTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerDeleteTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    /// <summary>
    /// Delete: AllowAnonymous yok; sınıf seviyesindeki CompanyAdmins geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Delete_WhenNoAllowAnonymous_RequiresAuthenticationViaControllerAuthorize()
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.Delete));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Delete anonim çağrıya açık olmamalı");

        method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).Should().BeEmpty();

        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);
        authorize.Roles.Should().Be($"{AppRoles.SuperAdmin},{AppRoles.CompanyAdmin}");
    }

    /// <summary>
    /// Delete: yalnızca SuperAdmin / CompanyAdmin; Manager / Staff Roles içinde yok.
    /// Senaryo 2 — claims var ama CompanyAdmins değil → filtre reddeder (403).
    /// Not: Metodu doğrudan çağırmak filtreyi atlar; bu sözleşme testidir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public void Delete_WhenRoleIsNotCompanyAdmin_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var method = typeof(UsersController).GetMethod(nameof(UsersController.Delete));
        method.Should().NotBeNull();
        method!.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Should().BeEmpty("Delete için metot seviyesinde roller genişletilmemeli");

        var authorize = typeof(UsersController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.CompanyAdmins);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().Contain(AppRoles.CompanyAdmin);
        allowed.Should().NotContain(role.ToString());
    }

    /// <summary>
    /// Delete: servis başarılı → NoContent(204).
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _userService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _userService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Delete: kullanıcı yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Delete_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _userService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"User bulunamadı: {id}"));

        var act = async () => await _sut.Delete(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User bulunamadı: {id}");
    }
}
