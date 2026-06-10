using ShopfloorManager.Domain.Entities;

namespace ShopfloorManager.Application.Users;

public record UserDto(
    int Id,
    string UserLogin,
    string Name,
    string? Email,
    string? Sex,
    string? Role,
    string? UserType,
    string? Position,
    int? RoleId,
    int? UserTypeId,
    int? PositionId,
    int? WorkStatusId,
    bool IsActive,
    bool FirstLogin,
    DateTimeOffset CreatedAt)
{
    public static UserDto From(User u) => new(
        u.Id, u.UserLogin, u.Name, u.Email, u.Sex,
        u.Role?.Name, u.UserType?.TypeName, u.Position?.Code,
        u.RoleId, u.UserTypeId, u.PositionId, u.WorkStatusId,
        u.IsActive, u.FirstLogin, u.CreatedAt);
}
