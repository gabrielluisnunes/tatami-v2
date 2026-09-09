using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tatami.Application.Storage;

namespace Tatami.Infrastructure.Integrations.Storage;

public class MinioBucketBootstrapHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MinioBucketBootstrapHostedService> _logger;

    public MinioBucketBootstrapHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MinioBucketBootstrapHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var options = scope.ServiceProvider.GetRequiredService<IOptions<MinioOptions>>().Value;
            var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

            await storage.EnsureBucketExistsAsync(options.BucketPhotos, cancellationToken);
            await storage.EnsureBucketExistsAsync(options.BucketContracts, cancellationToken);
            _logger.LogInformation(
                "MinIO buckets ready: {Photos}, {Contracts}",
                options.BucketPhotos,
                options.BucketContracts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "MinIO bucket bootstrap failed — photo upload will fail until MinIO is available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
