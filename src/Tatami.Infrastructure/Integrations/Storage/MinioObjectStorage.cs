using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Tatami.Application.Storage;

namespace Tatami.Infrastructure.Integrations.Storage;

public class MinioObjectStorage : IObjectStorage
{
    private readonly IAmazonS3 _s3;
    private readonly MinioPresignClient _presign;
    private readonly ILogger<MinioObjectStorage> _logger;

    public MinioObjectStorage(
        IAmazonS3 s3,
        MinioPresignClient presign,
        ILogger<MinioObjectStorage> logger)
    {
        _s3 = s3;
        _presign = presign;
        _logger = logger;
    }

    public async Task EnsureBucketExistsAsync(
        string bucket,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3, bucket);
            if (!exists)
            {
                await _s3.PutBucketAsync(bucket, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure MinIO bucket {Bucket} exists", bucket);
            throw;
        }
    }

    public async Task PutObjectAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
            CannedACL = S3CannedACL.Private,
        };

        await _s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task DeleteObjectAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3.DeleteObjectAsync(bucket, objectKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to delete MinIO object {Bucket}/{Key}",
                bucket,
                objectKey);
        }
    }

    public async Task<string> GetPresignedGetUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Protocol = _presign.Protocol,
        };

        return await _presign.S3.GetPreSignedURLAsync(request);
    }
}
