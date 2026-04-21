using Amazon.S3;
using Amazon.S3.Model;

namespace NarrationApp.Server.Services;

public sealed class AwsR2ObjectClient(IAmazonS3 s3Client) : IR2ObjectClient
{
    public async Task SaveAsync(string bucketName, string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = objectKey,
            InputStream = content,
            AutoCloseStream = false,
            ContentType = contentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };

        await s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        using var response = await s3Client.GetObjectAsync(bucketName, objectKey, cancellationToken);
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task DeleteAsync(string bucketName, string objectKey, CancellationToken cancellationToken = default)
    {
        await s3Client.DeleteObjectAsync(bucketName, objectKey, cancellationToken);
    }
}
