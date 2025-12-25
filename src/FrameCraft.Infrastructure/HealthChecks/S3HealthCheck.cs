using Amazon.S3;
using Amazon.S3.Model;
using FrameCraft.Application.Common.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FrameCraft.Infrastructure.HealthChecks;

public class S3HealthCheck : IHealthCheck
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3Settings _settings;

    public S3HealthCheck(IAmazonS3 s3Client, IOptions<S3Settings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = _settings.BucketName,
                MaxKeys = 1
            }, cancellationToken);

            return HealthCheckResult.Healthy($"S3 bucket '{_settings.BucketName}' is accessible");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return HealthCheckResult.Unhealthy($"S3 bucket '{_settings.BucketName}' not found");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"S3 connection failed: {ex.Message}");
        }
    }
}