namespace ShopfloorManager.Desktop.Services;

public interface IApiClient
{
    void SetToken(string? token);
    Task<ApiResponse<TResponse>?> GetAsync<TResponse>(string path, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
    Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default);
}

public record ApiResponse<T>(bool Success, T? Data, string? Error, PaginationMeta? Pagination);
public record PaginationMeta(int Page, int PageSize, int Total);
