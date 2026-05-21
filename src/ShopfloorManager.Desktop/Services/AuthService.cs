using System.Net.Http;
using System.Net.Http.Json;

namespace ShopfloorManager.Desktop.Services;

public class AuthService : IAuthService
{
    private readonly IApiClient _api;

    public string? Token { get; private set; }
    public int? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? Role { get; private set; }
    public bool IsAuthenticated => Token is not null;
    public bool FirstLogin { get; private set; }

    public AuthService(IApiClient api)
    {
        _api = api;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        try
        {
            var response = await _api.PostAsync<LoginRequest, LoginResponse>(
                "/api/v1/auth/login",
                new LoginRequest(username, password));

            if (response?.Data is null)
                return new LoginResult(false, "Thông tin đăng nhập không hợp lệ");

            Token = response.Data.Token;
            UserId = response.Data.UserId;
            UserName = response.Data.FullName;
            Role = response.Data.Role;
            FirstLogin = response.Data.FirstLogin;

            _api.SetToken(Token);
            return new LoginResult(true);
        }
        catch (HttpRequestException)
        {
            return new LoginResult(false, "Không thể kết nối tới server. Kiểm tra kết nối mạng.");
        }
        catch
        {
            return new LoginResult(false, "Đã xảy ra lỗi. Vui lòng thử lại.");
        }
    }

    public void Logout()
    {
        Token = null;
        UserId = null;
        UserName = null;
        Role = null;
        FirstLogin = false;
        _api.SetToken(null);
    }
}

file record LoginRequest(string Username, string Password);

file record LoginResponse(string Token, int UserId, string FullName, string Role, bool FirstLogin);
