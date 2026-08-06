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

/// <summary>UsersController.Update birim testleri.</summary>
public class UsersControllerUpdateTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UsersController _sut;

    public UsersControllerUpdateTests()
    {
        _sut = new UsersController(_userService.Object);
    }

    /// <summary>
    /// Update: servis başarılı UserResponse → Ok(200).
    /// </summary>
    [Fact]
    public async Task Update_WhenServiceSucceeds_ReturnsOkWithUserResponse()
    {
        var id = Guid.NewGuid();
        var request = UsersTestHelper.CreateUpdateRequest();
        var expected = UsersTestHelper.CreateUserResponse(
            id,
            request.CompanyId,
            request.Email,
            request.Role,
            request.IsActive);
        _userService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(expected);
        _userService.Verify(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Update: kullanıcı yok → KeyNotFoundException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = UsersTestHelper.CreateUpdateRequest();
        _userService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"User bulunamadı: {id}"));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User bulunamadı: {id}");
    }

    /// <summary>
    /// Update: e-posta boş → ValidationException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenEmailEmpty_ThrowsValidationException()
    {
        var id = Guid.NewGuid();
        var request = UsersTestHelper.CreateUpdateRequest(email: "");
        _userService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Email), "E-posta zorunludur.")
            ]));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Update: SuperAdmin atama yetkisi yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task Update_WhenSuperAdminAssignmentForbidden_ThrowsForbiddenException()
    {
        var id = Guid.NewGuid();
        var request = UsersTestHelper.CreateUpdateRequest(role: UserRole.SuperAdmin, companyId: null);
        _userService
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir."));

        var act = async () => await _sut.Update(id, request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
    }
}
