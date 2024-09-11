using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;

namespace Amazon.GenAI.Abstractions;

public class S3Wrapper
{
    private readonly IAmazonS3 _s3Client;

    public S3Wrapper()
    {
        _s3Client = new AmazonS3Client();
    }

    public S3Wrapper(RegionEndpoint region)
    {
        _s3Client = new AmazonS3Client(region);
    }

    public async Task<List<Dictionary<string, string>>> GetMetaDataFromS3(string bucketName, string prefix, string fileExtension)
    {
        var result = new List<Dictionary<string, string>>();

        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
        };

        ListObjectsV2Response response;
        do
        {
            response = await _s3Client.ListObjectsV2Async(request);

            foreach (var entry in response.S3Objects)
            {
                if (!entry.Key.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase)) continue;

                var metadata = await DownloadAndProcessFileAsync(bucketName, entry.Key);
                if (metadata != null)
                {
                    result.Add(metadata!);
                }
            }

            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated);

        return result;
    }

    private async Task<Dictionary<string, string?>?> DownloadAndProcessFileAsync(string bucketName, string key)
    {
        var request = new GetObjectRequest
        {
            BucketName = bucketName,
            Key = key
        };

        using var response = await _s3Client.GetObjectAsync(request);
        using var memoryStream = new MemoryStream();

        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return await ProcessJsonStreamAsync(memoryStream, key);
    }

    private static async Task<Dictionary<string, string?>?> ProcessJsonStreamAsync(MemoryStream memoryStream, string key)
    {
        using var doc = await JsonDocument.ParseAsync(memoryStream);

        var root = doc.RootElement;
        if (root.TryGetProperty("metadataAttributes", out var metadataAttributes))
        {
            var result = new Dictionary<string, string?>();
            foreach (var prop in metadataAttributes.EnumerateObject())
            {
                result[prop.Name] = prop.Value.GetString();
            }
            return result;
        }
        else
        {
            Console.WriteLine($"File: {key}, metadataAttributes not found in the JSON file.");
            return null;
        }
    }
}
