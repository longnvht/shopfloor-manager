using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
