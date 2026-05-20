using FluentResults;

namespace ShopfloorManager.API.Common;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public PaginationMeta? Pagination { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string error) => new() { Success = false, Error = error };

    public static ApiResponse<T> Fail(IEnumerable<IError> errors) =>
        Fail(string.Join("; ", errors.Select(e => e.Message)));
}

public record PaginationMeta(int Page, int PageSize, int Total, int TotalPages);
