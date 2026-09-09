namespace Tatami.Application.Storage;

public interface IObjectStorage
{
    Task EnsureBucketExistsAsync(string bucket, CancellationToken cancellationToken = default);

    Task PutObjectAsync(
        string bucket,
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(
        string bucket,
        string objectKey,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedGetUrlAsync(
        string bucket,
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
