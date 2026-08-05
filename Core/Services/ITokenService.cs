using Core.Entities;

namespace Core.Services;

/// <summary>JWT token üretir.</summary>
public interface ITokenService
{
    string CreateToken(User user, out DateTime expiresAt);
}
