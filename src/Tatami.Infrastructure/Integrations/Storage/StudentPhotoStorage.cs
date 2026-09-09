using Microsoft.Extensions.Options;
using Tatami.Application.Storage;

namespace Tatami.Infrastructure.Integrations.Storage;

public class StudentPhotoStorage : IStudentPhotoStorage
{
    private readonly IObjectStorage _objectStorage;
    private readonly MinioOptions _options;

    public StudentPhotoStorage(IObjectStorage objectStorage, IOptions<MinioOptions> options)
    {
        _objectStorage = objectStorage;
        _options = options.Value;
    }

    public Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default) =>
        _objectStorage.PutObjectAsync(
            _options.BucketPhotos,
            objectKey,
            content,
            contentType,
            cancellationToken);

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default) =>
        _objectStorage.DeleteObjectAsync(_options.BucketPhotos, objectKey, cancellationToken);

    public Task<string> GetSignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default) =>
        _objectStorage.GetPresignedGetUrlAsync(
            _options.BucketPhotos,
            objectKey,
            expiresIn,
            cancellationToken);
}
