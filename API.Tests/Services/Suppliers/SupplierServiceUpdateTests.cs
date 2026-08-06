using Core.Abstractions;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Suppliers;

/// <summary>SupplierService.UpdateAsync / GetByCompanyIdAsync / DeleteAsync birim testleri.</summary>
public class SupplierServiceUpdateTests
{
    private readonly Mock<ISupplierRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly SupplierService _sut;

    public SupplierServiceUpdateTests()
    {
        _sut = SupplierServiceTestHelper.CreateSut(_repository, _unitOfWork, _currentUser);
    }

    /// <summary>Update: CompanyName boş → ValidationException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenCompanyNameEmpty_ThrowsValidationException()
    {
        var request = SupplierServiceTestHelper.ValidUpdate(companyName: "");

        var act = async () => await _sut.UpdateAsync(Guid.NewGuid(), request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.CompanyName));
        _repository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Update: tedarikçi yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSupplierNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        var request = SupplierServiceTestHelper.ValidUpdate();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var act = async () => await _sut.UpdateAsync(id, request);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Supplier bulunamadı: {id}");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Update: geçerli istek → Update + SaveChanges + response.</summary>
    [Fact]
    public async Task UpdateAsync_WhenSuccessful_UpdatesSavesAndReturnsResponse()
    {
        var entity = SupplierServiceTestHelper.CreateEntity();
        var request = SupplierServiceTestHelper.ValidUpdate(
            companyName: "Yeni Tedarik",
            contactName: "Yeni Kişi",
            phone: "+905550000000",
            email: "yeni@tedarik.com",
            address: "İzmir",
            taxNumber: "1112223334");
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(entity.Id, request);

        entity.CompanyName.Should().Be("Yeni Tedarik");
        entity.ContactName.Should().Be("Yeni Kişi");
        entity.Email.Should().Be("yeni@tedarik.com");
        result.CompanyName.Should().Be("Yeni Tedarik");
        result.ContactName.Should().Be("Yeni Kişi");
        result.TaxNumber.Should().Be("1112223334");

        _repository.Verify(r => r.Update(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>GetByCompanyId: başka şirket → ForbiddenException.</summary>
    [Fact]
    public async Task GetByCompanyIdAsync_WhenOtherCompany_ThrowsForbiddenException()
    {
        var ownCompanyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        SupplierServiceTestHelper.SetupCompanyAdminCurrentUser(_currentUser, ownCompanyId);

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
        SupplierServiceTestHelper.SetupSuperAdminCurrentUser(_currentUser);
        var list = new List<Supplier>
        {
            SupplierServiceTestHelper.CreateEntity(companyId: companyId)
        };
        _repository
            .Setup(r => r.GetByCompanyIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _sut.GetByCompanyIdAsync(companyId);

        result.Should().HaveCount(1);
        result[0].CompanyId.Should().Be(companyId);
    }

    /// <summary>Delete: tedarikçi yok → KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSupplierNotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var act = async () => await _sut.DeleteAsync(id);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Supplier bulunamadı: {id}");
    }

    /// <summary>Delete: tedarikçi var → Remove + SaveChanges.</summary>
    [Fact]
    public async Task DeleteAsync_WhenSuccessful_RemovesAndSaves()
    {
        var entity = SupplierServiceTestHelper.CreateEntity();
        _repository
            .Setup(r => r.GetByIdAsync(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(entity.Id);

        _repository.Verify(r => r.Remove(entity), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
