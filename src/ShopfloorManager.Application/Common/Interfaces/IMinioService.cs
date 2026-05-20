namespace ShopfloorManager.Application.Common.Interfaces;

public interface IMinioService
{
    /// <summary>Tạo pre-signed URL để client upload thẳng lên MinIO.</summary>
    Task<string> GetUploadUrlAsync(string objectKey, int expirySeconds = 600, CancellationToken ct = default);

    /// <summary>Tạo pre-signed URL để download/xem file.</summary>
    Task<string> GetDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default);

    /// <summary>Xoá object khỏi MinIO.</summary>
    Task DeleteAsync(string objectKey, CancellationToken ct = default);

    /// <summary>
    /// Rename object trong MinIO (copy rồi xoá nguồn).
    /// Dùng khi upload đè lên file Rejected: rename "file.nc" → "Rejected_file.nc".
    /// </summary>
    Task RenameAsync(string oldKey, string newKey, CancellationToken ct = default);
}

/// <summary>
/// Build MinIO object key theo path convention từ FileType flags.
/// Path convention (phân tích từ FormUpdateTechnology.cs):
///   IsPartNumber: {PartNumber}/{RevCode}/{RoutingRevCode}/{OpNumber}/{Folder}/{filename}
///   IsJobNumber:  {JobNumber}/{OpNumber}/{Folder}/{filename}
/// </summary>
public static class MinioPathBuilder
{
    public static string BuildObjectKey(
        string folder, string fileName,
        string? partNumber = null, string? revCode = null, string? routingRevCode = null,
        string? opNumber = null, string? jobNumber = null,
        bool isPartNumber = true, bool isRevision = true,
        bool isOpNumber = false, bool isJobNumber = false)
    {
        var segments = new List<string>();

        if (isPartNumber && !string.IsNullOrWhiteSpace(partNumber))
        {
            segments.Add(partNumber);
            if (isRevision && !string.IsNullOrWhiteSpace(revCode))
            {
                segments.Add(revCode);
                if (!string.IsNullOrWhiteSpace(routingRevCode))
                    segments.Add(routingRevCode);
            }
            if (isOpNumber && !string.IsNullOrWhiteSpace(opNumber))
                segments.Add(opNumber);
        }
        else if (isJobNumber && !string.IsNullOrWhiteSpace(jobNumber))
        {
            segments.Add(jobNumber);
            if (isOpNumber && !string.IsNullOrWhiteSpace(opNumber))
                segments.Add(opNumber);
        }

        if (!string.IsNullOrWhiteSpace(folder))
            segments.Add(folder);

        segments.Add(fileName);
        return string.Join("/", segments);
    }
}
