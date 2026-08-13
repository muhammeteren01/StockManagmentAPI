using System.Security.Claims;
using API.Controllers;
using Core.Enums;
using Core.Services;
using FluentAssertions;
using Integration.Sysmond.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.Auth;

/// <summary>
/// AuthController.Me birim testleri ([Authorize] endpoint — claim/token alanları).
/// Eksik claim ve company_id yok senaryoları dahil.
/// </summary>
public class AuthControllerMeTests
{
    private readonly AuthController _sut = new(
        new Mock<IAuthService>().Object,
        new Mock<ISysmondTokenService>().Object);

    /// <summary>
    /// Me: tüm claim'ler (id, email, name, role, company_id) doluysa Ok gövdesinde hepsi görünür.
    /// </summary>
    [Fact]
    public void Me_WhenAllClaimsPresent_ReturnsOkWithAnonymousObject()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "ali@test.com"),
            new Claim(ClaimTypes.Name, "Ali Veli"),
            new Claim(ClaimTypes.Role, UserRole.Staff.ToString()),
            new Claim("company_id", companyId.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = "ali@test.com",
            name = "Ali Veli",
            role = UserRole.Staff.ToString(),
            companyId = companyId.ToString()
        });
    }

    /// <summary>
    /// Me: company_id claim yok (SuperAdmin token senaryosu) → companyId null, diğer alanlar dolu.
    /// </summary>
    [Fact]
    public void Me_WhenCompanyIdClaimMissing_ReturnsNullCompanyId()
    {
        var userId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "root@test.com"),
            new Claim(ClaimTypes.Name, "Root Admin"),
            new Claim(ClaimTypes.Role, UserRole.SuperAdmin.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = "root@test.com",
            name = "Root Admin",
            role = UserRole.SuperAdmin.ToString(),
            companyId = (string?)null
        });
    }

    /// <summary>
    /// Me: email claim yok → email null, diğer mevcut claim'ler dolu kalır.
    /// </summary>
    [Fact]
    public void Me_WhenEmailClaimMissing_ReturnsNullEmail()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "Ali Veli"),
            new Claim(ClaimTypes.Role, UserRole.Manager.ToString()),
            new Claim("company_id", companyId.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = (string?)null,
            name = "Ali Veli",
            role = UserRole.Manager.ToString(),
            companyId = companyId.ToString()
        });
    }

    /// <summary>
    /// Me: NameIdentifier (id) claim yok → id null.
    /// </summary>
    [Fact]
    public void Me_WhenUserIdClaimMissing_ReturnsNullId()
    {
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.Email, "ali@test.com"),
            new Claim(ClaimTypes.Role, UserRole.Staff.ToString()));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = (string?)null,
            email = "ali@test.com",
            name = (string?)null,
            role = UserRole.Staff.ToString(),
            companyId = (string?)null
        });
    }

    /// <summary>
    /// Me: role claim yok → role null.
    /// </summary>
    [Fact]
    public void Me_WhenRoleClaimMissing_ReturnsNullRole()
    {
        var userId = Guid.NewGuid();
        AuthTestHelper.SetUser(_sut,
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "ali@test.com"));

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = userId.ToString(),
            email = "ali@test.com",
            name = (string?)null,
            role = (string?)null,
            companyId = (string?)null
        });
    }

    /// <summary>
    /// Me: hiç claim yok → tüm alanlar null Ok gövdesi.
    /// </summary>
    [Fact]
    public void Me_WhenNoClaims_ReturnsOkWithAllFieldsNull()
    {
        AuthTestHelper.SetUser(_sut);

        var result = _sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new
        {
            id = (string?)null,
            email = (string?)null,
            name = (string?)null,
            role = (string?)null,
            companyId = (string?)null
        });
    }
}
