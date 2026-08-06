using Core.Entities;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Companies;

/// <summary>CompanyService.UpdateAsync / DeleteAsync birim testleri.</summary>
public class CompanyServiceUpdateTests
{
    private readonly Mock<ICompanyRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CompanyService _sut;

    public CompanyServiceUpdateTests()
    {
        _sut = CompanyServiceTestHelper.CreateSut(_repository, _unitOfWork);
    }

    /// <summary>Update: Name boş → ValidationException; GetById çağrılmaz.</summary>
    [Fact]
    public async Task UpdateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = CompanyServiceTestHelper.ValidUpdate(name: "");

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Update: şirket yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = CompanyServiceTestHelper.ValidUpdate();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Company bulunamadı: {id}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: geçerli istek → Update + SaveChanges + güncellenmiş response.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSuccessful_UpdatesSavesAndReturnsResponse()
    {
        var entity = CompanyServiceTestHelper.CreateEntity();
        var request = CompanyServiceTestHelper.ValidUpdate(name: "Yeni Unvan", isActive: false);
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        entity.Name.Should().Be("Yeni Unvan");
        entity.IsActive.Should().BeFalse();
        entity.Email.Should().Be(request.Email);
        result.Name.Should().Be("Yeni Unvan");
        result.IsActive.Should().BeFalse();

        _repository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Delete: şirket yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteAsync_WhenCompanyNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Company?)null);

        var act = async () => await _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Company bulunamadı: {id}");
        _repository.Verify(r => r.Remove(It.IsAny<Company>()), Times.Never);
    }

    /// <summary>Delete: şirket var → Remove + SaveChanges.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccessful_RemovesAndSaves()
    {
        var entity = CompanyServiceTestHelper.CreateEntity();
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(entity.Id);

        _repository.Verify(r => r.Remove(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
