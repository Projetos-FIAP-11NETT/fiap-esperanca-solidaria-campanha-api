using Amazon.S3;
using Amazon.S3.Model;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Storage;
using Microsoft.Extensions.Options;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Storage;

public class S3ImageStorageService(IAmazonS3 s3Client, IOptions<S3Settings> options) : IImageStorageService
{
    private readonly S3Settings _settings = options.Value;
    private bool _bucketEnsured;

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var key = $"{Guid.NewGuid()}-{Path.GetFileName(fileName)}";

        await s3Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead,
        }, cancellationToken);

        var baseUrl = string.IsNullOrWhiteSpace(_settings.PublicBaseUrl)
            ? _settings.ServiceUrl
            : _settings.PublicBaseUrl;

        return $"{baseUrl.TrimEnd('/')}/{_settings.BucketName}/{key}";
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketEnsured) return;

        try
        {
            await s3Client.PutBucketAsync(new PutBucketRequest { BucketName = _settings.BucketName }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
        {
            // Bucket já existe de uma execução anterior — segue o baile.
        }

        _bucketEnsured = true;
    }
}
