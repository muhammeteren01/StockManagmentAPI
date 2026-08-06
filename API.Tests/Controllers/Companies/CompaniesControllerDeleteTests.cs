using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Companies;

/// <summary>CompaniesController.Delete birim testleri.</summary>
public class CompaniesControllerDeleteTests
{
    private readonly Mock<ICompanyService> _companyService = new();
    private readonly CompaniesController _sut;

    public CompaniesControllerDeleteTests()
    {
        _sut = new CompaniesController(_companyService.Object);
    }

    /// <summary>
    /// Delete: AllowAnonymous yok; sınıf seviyesindeki SuperAdminOnly geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Delete_WhenNoAllowAnonymous_RequiresAuthenticationViaControllerAuthorize()
    {
        var method = typeof(CompaniesController).GetMethod(nameof(CompaniesController.Delete));
        method.Should().NotBeNull();

        method!.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true)
            .Should().BeEmpty("Delete anonim çağrıya açık olmamalı");

        // Metot seviyesinde genişletme yok; controller Authorize miras alınır.
        method.GetCustomAttributes<AuthorizeAttribute>(inherit: false).Should().BeEmpty();

        var authorize = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.SuperAdminOnly);
        authorize.Roles.Should().Be(AppRoles.SuperAdmin);
    }

    /// <summary>
    /// Delete: yalnızca SuperAdmin; CompanyAdmin / Manager / Staff Roles içinde yok.
    /// Senaryo 2 — claims var ama SuperAdmin değil → filtre reddeder (403).
    /// Not: Metodu doğrudan çağırmak filtreyi atlar; bu sözleşme testidir.
    /// </summary>
    [Theory]
    [InlineData(UserRole.CompanyAdmin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Staff)]
    public void Delete_WhenRoleIsNotSuperAdmin_RoleIsExcludedFromAuthorizeRoles(UserRole role)
    {
        var method = typeof(CompaniesController).GetMethod(nameof(CompaniesController.Delete));
        method.Should().NotBeNull();
        method!.GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Should().BeEmpty("Delete için metot seviyesinde roller genişletilmemeli");

        var authorize = typeof(CompaniesController).GetCustomAttribute<AuthorizeAttribute>(inherit: true);
        authorize.Should().NotBeNull();
        authorize!.Roles.Should().Be(AppRoles.SuperAdminOnly);

        var allowed = authorize.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        allowed.Should().Contain(AppRoles.SuperAdmin);
        allowed.Should().NotContain(role.ToString());
    }

    /// <summary>
    /// Delete: servis başarılı olduğunda NoContent(204);
    /// DeleteAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task Delete_WhenServiceSucceeds_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _companyService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _companyService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Delete: şirket yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Delete_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _companyService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Company bulunamadı: {id}"));

        var act = async () => await _sut.Delete(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Company bulunamadı: {id}");
    }
}
