namespace ShopfloorManager.Application.Common.Interfaces;

public interface IMinioService
{
    /// <summary>Tạo pre-signed URL để client upload thẳng lên MinIO.</summary>
    Task<string> GetUploadUrlAsync(string objectKey, int expirySeconds = 600, CancellationToken ct = default);

    /// <summary>Tạo pre-signed URL để download/xem file.</summary>
    Task<string> GetDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default);

    /// <summary>Xoá object khỏi MinIO.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}
