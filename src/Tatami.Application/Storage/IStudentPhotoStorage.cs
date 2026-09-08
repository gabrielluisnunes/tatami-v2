namespace Tatami.Application.Storage;

/// <summary>Student photo operations against the private photos bucket.</summary>
public interface IStudentPhotoStorage
{
    Task UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    Task<string> GetSignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);
}
