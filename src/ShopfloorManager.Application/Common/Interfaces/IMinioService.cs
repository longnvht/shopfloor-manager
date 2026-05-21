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
/// Build MinIO object key theo path convention — spec 05_technical_documents.md:
///   Part-level  (DRW, CAD):       {folder}/{part_number}/{revision}/{filename}
///   Standard OP (GCD, TLS...):    {folder}/{part_number}/{op_number}/{revision}/{filename}
///   Job+OP      (RTC, FXT):       {folder}/{job_number}/{op_number}/{filename}
/// </summary>
public static class MinioPathBuilder
{
    public static string BuildObjectKey(
        string folder, string fileName,
        string? partNumber = null, string? revCode = null,
        string? opNumber = null, string? jobNumber = null,
        bool isPartNumber = false, bool isRevision = false,
        bool isOpNumber = false, bool isJobNumber = false)
    {
        var segments = new List<string> { folder };

        if (isJobNumber && !string.IsNullOrWhiteSpace(jobNumber))
        {
            // Job+OP path: {folder}/{job_number}/{op_number}/{filename}
            segments.Add(jobNumber);
            if (isOpNumber && !string.IsNullOrWhiteSpace(opNumber))
                segments.Add(opNumber);
        }
        else if (isPartNumber && !string.IsNullOrWhiteSpace(partNumber))
        {
            // Part or Part+OP path
            segments.Add(partNumber);
            if (isOpNumber && !string.IsNullOrWhiteSpace(opNumber))
                segments.Add(opNumber);
            if (isRevision && !string.IsNullOrWhiteSpace(revCode))
                segments.Add(revCode);
        }

        segments.Add(fileName);
        return string.Join("/", segments);
    }
}
