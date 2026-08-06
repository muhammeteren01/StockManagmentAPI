using System.Reflection;
using API.Controllers;
using Core.Authorization;
using Core.Enums;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Categories;

/// <summary>CategoriesController.Delete birim testleri.</summary>
public class CategoriesControllerDeleteTests
{
    private readonly Mock<ICategoryService> _categoryService = new();
    private readonly CategoriesController _sut;

    public CategoriesControllerDeleteTests()
    {
        _sut = new CategoriesController(_categoryService.Object);
    }

    /// <summary>
    /// Delete: AllowAnonymous yok; metot seviyesinde Writers Roles geçerli.
    /// Senaryo 1 — claims yok / anonim: AllowAnonymous olmadığı için pipeline kimlik ister (401/403).
    /// </summary>
    [Fact]
    public void Delete_WhenNoAllowAnonymous_RequiresAuthenticationViaAuthorize()
    {
        var method = typeof(CategoriesController).GetMethod(nameof(CategoriesController.Delete));
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
        var method = typeof(CategoriesController).GetMethod(nameof(CategoriesController.Delete));
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
        _categoryService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _categoryService.Verify(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Delete: kategori yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Delete_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _categoryService
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Category bulunamadı: {id}"));

        var act = async () => await _sut.Delete(id, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Category bulunamadı: {id}");
    }
}
