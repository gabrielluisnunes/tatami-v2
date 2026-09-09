namespace Tatami.Infrastructure.Integrations.Storage;

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string Endpoint { get; set; } = "http://localhost:9000";

    public string PublicEndpoint { get; set; } = "";

    public string AccessKey { get; set; } = "tatami_minio";

    public string SecretKey { get; set; } = "tatami_minio_dev";

    public string BucketPhotos { get; set; } = "tatami-photos";

    public string BucketContracts { get; set; } = "tatami-contracts";

    public bool ForcePathStyle { get; set; } = true;
}
