using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Tatami.Infrastructure.Integrations.Storage;

internal static class MinioS3Factory
{
    public static AmazonS3Client CreateClient(MinioOptions options, string endpoint)
    {
        AWSConfigsS3.UseSignatureVersion4 = true;

        var uri = ParseEndpoint(endpoint);
        var config = new AmazonS3Config
        {
            ServiceURL = uri.GetLeftPart(UriPartial.Authority),
            ForcePathStyle = options.ForcePathStyle,
            AuthenticationRegion = "us-east-1",
            UseHttp = uri.Scheme == Uri.UriSchemeHttp,
        };

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        return new AmazonS3Client(credentials, config);
    }

    public static string ResolvePublicEndpoint(MinioOptions options) =>
        string.IsNullOrWhiteSpace(options.PublicEndpoint)
            ? options.Endpoint
            : options.PublicEndpoint.Trim();

    public static bool SameEndpoint(string left, string right) =>
        string.Equals(
            ParseEndpoint(left).GetLeftPart(UriPartial.Authority).TrimEnd('/'),
            ParseEndpoint(right).GetLeftPart(UriPartial.Authority).TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);

    public static Protocol ProtocolOf(string endpoint) =>
        ParseEndpoint(endpoint).Scheme == Uri.UriSchemeHttps
            ? Protocol.HTTPS
            : Protocol.HTTP;

    private static Uri ParseEndpoint(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"MinIO endpoint '{endpoint}' is invalid. Use an absolute http(s) URL.");
        }

        return uri;
    }
}
