namespace ShopfloorManager.Desktop.Services;

public interface IAuthService
{
    string? Token { get; }
    int? UserId { get; }
    string? UserName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool FirstLogin { get; }

    Task<LoginResult> LoginAsync(string username, string password);
    void Logout();
}

public record LoginResult(bool Success, string? Error = null);
