using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Categories;

/// <summary>CategoryService.UpdateAsync / GetByCompanyIdAsync / DeleteAsync birim testleri.</summary>
public class CategoryServiceUpdateTests
{
    private readonly Mock<ICategoryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly CategoryService _sut;

    public CategoryServiceUpdateTests()
    {
        _sut = CategoryServiceTestHelper.CreateSut(_repository, _unitOfWork, _currentUser);
    }

    /// <summary>Update: Name boş → ValidationException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = CategoryServiceTestHelper.ValidUpdate(name: "");

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Update: kategori yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCategoryNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = CategoryServiceTestHelper.ValidUpdate();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Category bulunamadı: {id}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: geçerli istek → Update + SaveChanges + response.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSuccessful_UpdatesSavesAndReturnsResponse()
    {
        var entity = CategoryServiceTestHelper.CreateEntity();
        var request = CategoryServiceTestHelper.ValidUpdate(
            name: "Yeni Kategori",
            description: "Yeni açıklama");
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        entity.Name.Should().Be("Yeni Kategori");
        entity.Description.Should().Be("Yeni açıklama");
        result.Name.Should().Be("Yeni Kategori");
        result.Description.Should().Be("Yeni açıklama");

        _repository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompanyId: başka şirket → ForbiddenException.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenOtherCompany_ThrowsForbiddenException()
    {
        var ownCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        CategoryServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, ownCompanyId);

        var act = async () => await _sut.GetByCompanyIdAsync(otherCompanyId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
        _repository.Verify(
            r => r.GetByCompanyIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>GetByCompanyId: SuperAdmin herhangi bir şirkete erişebilir.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenSuperAdmin_ReturnsList()
    {
        var companyId = Guid.NewGuid();
        CategoryServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var list = new List<Category>
        {
            CategoryServiceTestHelper.CreateEntity(companyId: companyId)
        };
        _repository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _sut.GetByCompanyIdAsync(companyId);

        result.Should().HaveCount(1);
        result[0].CompanyId.Should().Be(companyId);
    }

    /// <summary>Delete: kategori yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteAsync_WhenCategoryNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var act = async () => await _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Category bulunamadı: {id}");
    }

    /// <summary>Delete: kategori var → Remove + SaveChanges.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccessful_RemovesAndSaves()
    {
        var entity = CategoryServiceTestHelper.CreateEntity();
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(entity.Id);

        _repository.Verify(r => r.Remove(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
