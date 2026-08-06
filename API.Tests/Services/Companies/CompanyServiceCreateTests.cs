using Core.Entities;
using Core.Repositories;
using Core.UnitOfWork;
using Core.Validations;
using FluentAssertions;
using Moq;
using Service.Services;

namespace API.Tests.Services.Companies;

/// <summary>CompanyService.CreateAsync birim testleri (gerçek validator + mock bağımlılıklar).</summary>
public class CompanyServiceCreateTests
{
    private readonly Mock<ICompanyRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly CompanyService _sut;

    public CompanyServiceCreateTests()
    {
        _sut = CompanyServiceTestHelper.CreateSut(_repository, _unitOfWork);
    }

    /// <summary>Create: Name boş → ValidationException; repository çağrılmaz.</summary>
    [Fact]
    public async Task CreateAsync_WhenNameEmpty_ThrowsValidationException()
    {
        var request = CompanyServiceTestHelper.ValidCreate(name: "");

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Name));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: geçersiz e-posta → ValidationException.</summary>
    [Fact]
    public async Task CreateAsync_WhenEmailInvalid_ThrowsValidationException()
    {
        var request = CompanyServiceTestHelper.ValidCreate();
        request.Email = "not-an-email";

        var act = async () => await _sut.CreateAsync(request);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Errors.Should().ContainKey(nameof(request.Email));
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>Create: geçerli istek → AddAsync + SaveChanges + CompanyResponse.</summary>
    [Fact]
    public async Task CreateAsync_WhenSuccessful_AddsSavesAndReturnsResponse()
    {
        var request = CompanyServiceTestHelper.ValidCreate();
        Company? added = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()))
            .Callback<Company, CancellationToken>((c, _) => added = c)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await _sut.CreateAsync(request);

        added.Should().NotBeNull();
        added!.Name.Should().Be(request.Name);
        added.TaxOffice.Should().Be(request.TaxOffice);
        added.Email.Should().Be(request.Email);
        added.IsActive.Should().BeTrue();
        result.Id.Should().Be(added.Id);
        result.Name.Should().Be(request.Name);
        result.Email.Should().Be(request.Email);

        _repository.Verify(r => r.AddAsync(It.IsAny<Company>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
