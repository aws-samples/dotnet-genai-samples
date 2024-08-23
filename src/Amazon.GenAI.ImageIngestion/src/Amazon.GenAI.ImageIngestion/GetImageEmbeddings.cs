using Amazon.BedrockRuntime;
using Amazon.Lambda.Core;
using Amazon.S3;
using System.Text.Json;
using Amazon.GenAI.ImageIngestion.Abstractions;
using Amazon.GenAI.ImageIngestion.Abstractions.Splitter;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageEmbeddings
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");


    public async Task<Dictionary<string, object>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageEmbeddings.  destination: {_destinationBucket}");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        if (!input.TryGetValue("inference", out var inference))
        {
            throw new ArgumentException("Image inference not provided in the input.");
        }

        Console.WriteLine("inference");
        Console.WriteLine(inference);

        try
        {
            // Download the image from S3
            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key)) ?? "";
            var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);

            var content = inference.Trim();
            var chunkSize = 4000;
            var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: chunkSize);
            var splitText = textSplitter.SplitText("     ");
            var embeddings = new List<float[]>(capacity: splitText.Count);

            var embeddingModelId = "amazon.titan-embed-image-v1";
            var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), embeddingModelId);
            var embeddingsAsync = await embeddingModel.CreateEmbeddingsAsync(content, image);

            var embedding = embeddingsAsync?["embedding"]?.AsArray();
            if (embedding == null) return null;

            context.Logger.LogInformation($"GetImageEmbeddings got embeddings");
            
            var f = new float[1024];
            for (var j = 0; j < embedding.Count; j++)
            {
                f[j] = (float)embedding[j]?.AsValue()!;
            }

            embeddings.Add(f);

            return new Dictionary<string, object>
            {
                { "key", key },
                { "embeddings", embeddings },
                { "inference", inference },
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting embeddings: {e.Message}");
            throw;
        }
    }
}