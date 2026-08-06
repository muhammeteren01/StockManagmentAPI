using Core.Abstractions;
using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Users;

/// <summary>UserService.UpdateAsync / GetByCompanyIdAsync / DeleteAsync birim testleri.</summary>
public class UserServiceUpdateTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordService> _passwordService = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly UserService _sut;

    public UserServiceUpdateTests()
    {
        _sut = UserServiceTestHelper.CreateSut(
            _repository, _unitOfWork, _passwordService, _currentUser);
    }

    /// <summary>Update: FirstName boş → ValidationException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenFirstNameEmpty_ThrowsValidationException()
    {
        var request = UserServiceTestHelper.ValidUpdate();
        request.FirstName = "";

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.FirstName));
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Update: kullanıcı yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, Guid.NewGuid());
        var request = UserServiceTestHelper.ValidUpdate();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User bulunamadı: {id}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: CompanyAdmin SuperAdmin rolü atayamaz → ForbiddenException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCompanyAdminAssignsSuperAdmin_ThrowsForbiddenException()
    {
        var entity = UserServiceTestHelper.CreateEntity();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, entity.CompanyId!.Value);
        var request = UserServiceTestHelper.ValidUpdate(role: UserRole.SuperAdmin);
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var act = async () => await _sut.UpdateAsync(entity.Id, request);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("SuperAdmin rolü yalnızca SuperAdmin tarafından atanabilir.");
        _repository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    /// <summary>Update: CompanyAdmin — CompanyId token'dan; Update + SaveChanges.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCompanyAdminSuccessful_UpdatesSavesAndReturnsResponse()
    {
        var companyId = Guid.NewGuid();
        var entity = UserServiceTestHelper.CreateEntity(companyId: companyId);
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var request = UserServiceTestHelper.ValidUpdate(
            email: "updated@test.com",
            role: UserRole.Manager,
            companyId: null);
        request.CompanyId = null;
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        entity.Email.Should().Be("updated@test.com");
        entity.Role.Should().Be(UserRole.Manager);
        entity.CompanyId.Should().Be(companyId);
        result.Email.Should().Be("updated@test.com");
        result.CompanyId.Should().Be(companyId);

        _repository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompanyId: başka şirket → ForbiddenException.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenOtherCompany_ThrowsForbiddenException()
    {
        var ownCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, ownCompanyId);

        var act = async () => await _sut.GetByCompanyIdAsync(otherCompanyId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
        _repository.Verify(
            r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>GetByCompanyId: kendi şirket → liste döner.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenOwnCompany_ReturnsList()
    {
        var companyId = Guid.NewGuid();
        UserServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, companyId);
        var users = new List<User>
        {
            UserServiceTestHelper.CreateEntity(companyId: companyId)
        };
        _repository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _sut.GetByCompanyIdAsync(companyId);

        result.Should().HaveCount(1);
        result[0].CompanyId.Should().Be(companyId);
    }

    /// <summary>Delete: kullanıcı yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = async () => await _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"User bulunamadı: {id}");
    }

    /// <summary>Delete: kullanıcı var → Remove + SaveChanges.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccessful_RemovesAndSaves()
    {
        var entity = UserServiceTestHelper.CreateEntity();
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(entity.Id);

        _repository.Verify(r => r.Remove(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
