using Amazon.S3;
using Amazon.S3.Model;

namespace Tatami.Infrastructure.Integrations.Storage;

public sealed class MinioPresignClient
{
    public MinioPresignClient(IAmazonS3 s3, Protocol protocol)
    {
        S3 = s3;
        Protocol = protocol;
    }

    public IAmazonS3 S3 { get; }

    public Protocol Protocol { get; }
}
