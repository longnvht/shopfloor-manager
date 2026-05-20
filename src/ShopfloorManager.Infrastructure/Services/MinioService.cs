using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using ShopfloorManager.Application.Common.Interfaces;

namespace ShopfloorManager.Infrastructure.Services;

public class MinioService(IMinioClient minio, IConfiguration config, ILogger<MinioService> logger)
    : IMinioService
{
    private string Bucket => config["Minio:Bucket"] ?? "shopfloor-storage";

    public async Task<string> GetUploadUrlAsync(string objectKey, int expirySeconds = 600, CancellationToken ct = default)
    {
        await EnsureBucketAsync(ct);
        return await minio.PresignedPutObjectAsync(
            new PresignedPutObjectArgs().WithBucket(Bucket).WithObject(objectKey).WithExpiry(expirySeconds));
    }

    public async Task<string> GetDownloadUrlAsync(string objectKey, int expirySeconds = 3600, CancellationToken ct = default)
    {
        return await minio.PresignedGetObjectAsync(
            new PresignedGetObjectArgs().WithBucket(Bucket).WithObject(objectKey).WithExpiry(expirySeconds));
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        await minio.RemoveObjectAsync(
            new RemoveObjectArgs().WithBucket(Bucket).WithObject(objectKey), ct);
    }

    private async Task EnsureBucketAsync(CancellationToken ct)
    {
        var exists = await minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(Bucket), ct);
        if (!exists)
        {
            await minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(Bucket), ct);
            logger.LogInformation("Tạo bucket MinIO: {Bucket}", Bucket);
        }
    }
}
