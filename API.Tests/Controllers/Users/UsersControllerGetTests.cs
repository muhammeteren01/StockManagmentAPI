using API.Controllers;
using Core.DTOs.Users;
using Core.Exceptions;
using Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Users;

/// <summary>UsersController okuma endpoint'leri birim testleri.</summary>
public class UsersControllerGetTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerGetTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    /// <summary>
    /// GetAll: servis liste döndüğünde Ok(200) ve aynı body;
    /// GetAllAsync bir kez çağrılır.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var expected = new List<UserResponse>
        {
            UsersTestHelper.CreateUserResponse(email: "a@test.com"),
            UsersTestHelper.CreateUserResponse(email: "b@test.com")
        };
        _userService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetAll: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetAll_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _userService
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserResponse>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<UserResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetById: kullanıcı bulunduğunda Ok(200).
    /// </summary>
    [Fact]
    public async Task GetById_WhenFound_ReturnsOkWithUserResponse()
    {
        var id = Guid.NewGuid();
        var expected = UsersTestHelper.CreateUserResponse(id);
        _userService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetById(id, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetById: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetById_WhenNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _userService
            .Setup(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.GetById(id, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _userService.Verify(s => s.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: servis liste döndüğünde Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenServiceSucceeds_ReturnsOkWithList()
    {
        var companyId = Guid.NewGuid();
        var expected = new List<UserResponse>
        {
            UsersTestHelper.CreateUserResponse(companyId: companyId)
        };
        _userService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByCompany: boş liste Ok(200) ve boş dizi.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenEmpty_ReturnsOkWithEmptyList()
    {
        var companyId = Guid.NewGuid();
        _userService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserResponse>());

        var result = await _sut.GetByCompany(companyId, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IReadOnlyList<UserResponse>>()
            .Which.Should().BeEmpty();
    }

    /// <summary>
    /// GetByCompany: tenant erişim yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task GetByCompany_WhenAccessDenied_ThrowsForbiddenException()
    {
        var companyId = Guid.NewGuid();
        _userService
            .Setup(s => s.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.GetByCompany(companyId, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }

    /// <summary>
    /// GetByEmail: bulunduğunda Ok(200).
    /// </summary>
    [Fact]
    public async Task GetByEmail_WhenFound_ReturnsOkWithUserResponse()
    {
        const string email = "ali@test.com";
        var expected = UsersTestHelper.CreateUserResponse(email: email);
        _userService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByEmail(email, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// GetByEmail: servis null → NotFound(404).
    /// </summary>
    [Fact]
    public async Task GetByEmail_WhenNotFound_ReturnsNotFound()
    {
        const string email = "yok@test.com";
        _userService
            .Setup(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserResponse?)null);

        var result = await _sut.GetByEmail(email, CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
        _userService.Verify(s => s.GetByEmailAsync(email, It.IsAny<CancellationToken>()), Times.Once);
    }
}
