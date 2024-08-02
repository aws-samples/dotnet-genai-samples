using Amazon.BedrockRuntime;
using Amazon.GenAI.ImageIngestionLambda.Abstractions;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using OpenSearch.Client;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Amazon.GenAI.ImageIngestionLambda;

public class AddToVectorDb
{
    private readonly string _destinationBucket = Environment.GetEnvironmentVariable("destinationBucketName");
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();

    public async Task<Dictionary<string, string>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in AddDocumentToVectorDb");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {key}");
        context.Logger.LogInformation($"_destinationBucket: {_destinationBucket}");

        var embeddingModelId = "amazon.titan-embed-image-v1";
        var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), embeddingModelId);
        const int chuckSize = 10_000;
        var bulkDescriptor = new BulkDescriptor();

        try
        {
            var response = await _s3Client.GetObjectAsync(_destinationBucket, key);

            return new Dictionary<string, string>
            {
                { "key", key },
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting inference: {e.Message}");
            throw;
        }
    }
}