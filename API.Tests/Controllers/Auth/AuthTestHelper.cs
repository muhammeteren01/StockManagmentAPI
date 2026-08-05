using System.Security.Claims;
using Core.DTOs.Auth;
using Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Tests.Controllers.Auth;

/// <summary>Auth controller testleri için ortak yardımcılar.</summary>
internal static class AuthTestHelper
{
    public static AuthResponse CreateAuthResponse(string email, UserRole role, Guid? companyId) => new()
    {
        Token = "jwt-token",
        ExpiresAt = DateTime.UtcNow.AddHours(1),
        UserId = Guid.NewGuid(),
        CompanyId = companyId,
        Email = email,
        FirstName = "Ali",
        LastName = "Veli",
        Role = role
    };

    public static void SetUser(ControllerBase controller, params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
    }
}
