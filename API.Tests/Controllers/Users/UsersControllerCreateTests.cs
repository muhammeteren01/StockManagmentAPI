using API.Controllers;
using Core.Enums;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Users;

/// <summary>UsersController.Create birim testleri.</summary>
public class UsersControllerCreateTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerCreateTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    /// <summary>
    /// Create: servis başarılı UserResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithUserResponse()
    {
        var request = UsersTestHelper.CreateCreateRequest();
        var expected = UsersTestHelper.CreateUserResponse(
            email: request.Email,
            role: request.Role,
            companyId: request.CompanyId);
        _userService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UsersController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create: e-posta boş → ValidationException iletilir.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_WhenEmailEmptyOrNull_ThrowsValidationException(string? email)
    {
        var request = UsersTestHelper.CreateCreateRequest(email: email!);
        _userService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Email), "E-posta zorunludur.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Create: e-posta zaten kayıtlı → InvalidOperationException iletilir (UserService davranışı).
    /// </summary>
    [Fact]
    public async Task Create_WhenEmailAlreadyRegistered_ThrowsInvalidOperationException()
    {
        var request = UsersTestHelper.CreateCreateRequest(email: "ali@test.com");
        _userService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Bu e-posta zaten kayıtlı: {request.Email}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Bu e-posta zaten kayıtlı: {request.Email}");
    }

    /// <summary>
    /// Create: SuperAdmin atama yetkisi yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenSuperAdminAssignmentForbidden_ThrowsForbiddenException()
    {
        var request = UsersTestHelper.CreateCreateRequest(role: UserRole.SuperAdmin, companyId: null);
        _userService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
    }
}
