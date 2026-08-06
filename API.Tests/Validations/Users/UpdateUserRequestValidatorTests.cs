using Core.DTOs.Users;
using Core.Enums;
using Core.Validations.Users;
using FluentAssertions;

namespace API.Tests.Validations.Users;

/// <summary>UpdateUserRequestValidator birim testleri.</summary>
public class UpdateUserRequestValidatorTests
{
    private readonly UpdateUserRequestValidator _sut = new();

    private static UpdateUserRequest ValidRequest() => new()
    {
        FirstName = "Ali",
        LastName = "Yılmaz",
        Email = "ali.updated@test.com",
        CompanyId = Guid.NewGuid(),
        Role = UserRole.Manager,
        IsActive = true
    };

    /// <summary>Geçerli istek → doğrulama başarılı.</summary>
    [Fact]
    public void Validate_WhenRequestValid_Succeeds()
    {
        var result = _sut.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>FirstName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenFirstNameEmpty_ReturnsError(string? firstName)
    {
        var request = ValidRequest();
        request.FirstName = firstName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.FirstName));
    }

    /// <summary>LastName boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenLastNameEmpty_ReturnsError(string? lastName)
    {
        var request = ValidRequest();
        request.LastName = lastName!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.LastName));
    }

    /// <summary>E-posta boş → hata.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_WhenEmailEmpty_ReturnsError(string? email)
    {
        var request = ValidRequest();
        request.Email = email!;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.Email));
    }

    /// <summary>Geçersiz e-posta formatı → hata.</summary>
    [Fact]
    public void Validate_WhenEmailFormatInvalid_ReturnsError()
    {
        var request = ValidRequest();
        request.Email = "not-an-email";

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.Email));
    }

    /// <summary>Geçersiz Role enum → hata.</summary>
    [Fact]
    public void Validate_WhenRoleInvalid_ReturnsError()
    {
        var request = ValidRequest();
        request.Role = (UserRole)999;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRequest.Role));
    }

    /// <summary>CompanyId null → geçerli (TenantGuard serviste çözer).</summary>
    [Fact]
    public void Validate_WhenCompanyIdNull_Succeeds()
    {
        var request = ValidRequest();
        request.CompanyId = null;

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
