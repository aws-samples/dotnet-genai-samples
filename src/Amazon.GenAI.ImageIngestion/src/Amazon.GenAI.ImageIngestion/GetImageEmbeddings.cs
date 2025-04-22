using Amazon.BedrockRuntime;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.GenAI.ImageIngestion.Abstractions;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageEmbeddings
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    private const int Dimensions = 1024;

    public async Task<Dictionary<string, object>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        if (!input.TryGetValue("origBucketName", out var origBucketName))
        {
            throw new ArgumentException("origBucketName not provided in the input.");
        }

        try
        {
            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key)) ?? "";
            var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);

            var embeddings = new List<float[]>();
            var prompt = "describe image";
            var embeddingModelId = "amazon.titan-embed-image-v1";
            var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), embeddingModelId);
            var embeddingsAsync = await embeddingModel.CreateEmbeddingsAsync(prompt, image);

            var embedding = embeddingsAsync?["embedding"]?.AsArray();
            if (embedding == null) return null;
            
            var f = new float[Dimensions];
            for (var j = 0; j < embedding.Count; j++)
            {
                f[j] = (float)embedding[j]?.AsValue()!;
            }

            embeddings.Add(f);

            return new Dictionary<string, object>
            {
                { "key", key },
                { "embeddings", embeddings },
                { "origBucketName", origBucketName },
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting embeddings: {e.Message}");
            throw;
        }
    }
}