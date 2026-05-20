using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Users;

public record UserDto(
    int Id,
    string UserLogin,
    string Name,
    string? Email,
    string? Role,
    string? UserType,
    string? Position,
    bool IsActive,
    bool FirstLogin,
    DateTimeOffset CreatedAt)
{
    public static UserDto From(User u) => new(
        u.Id, u.UserLogin, u.Name, u.Email,
        u.Role?.Name, u.UserType?.TypeName, u.Position?.Code,
        u.IsActive, u.FirstLogin, u.CreatedAt);
}
