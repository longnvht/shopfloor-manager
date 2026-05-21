using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopfloorManager.Desktop.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetToken(string? token)
    {
        if (token is null)
            _http.DefaultRequestHeaders.Authorization = null;
        else
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResponse<TResponse>?> GetAsync<TResponse>(string path, CancellationToken ct = default)
    {
        var response = await _http.GetAsync(path, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync(path, body, JsonOptions, ct);
        return await ReadAsync<TResponse>(response, ct);
    }

    private static async Task<ApiResponse<TResponse>?> ReadAsync<TResponse>(HttpResponseMessage response, CancellationToken ct)
    {
        return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(JsonOptions, ct);
    }
}
