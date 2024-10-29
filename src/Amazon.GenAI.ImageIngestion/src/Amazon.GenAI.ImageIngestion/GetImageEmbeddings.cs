using Amazon.BedrockRuntime;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.GenAI.ImageIngestion.Abstractions;
using Amazon.GenAI.ImageIngestion.Abstractions.Splitter;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageEmbeddings
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    private static readonly int Dimensions = 1024;
    private static readonly int ChunkSize = 2000;

    public async Task<Dictionary<string, object>?> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageEmbeddings.  destination: {_destinationBucket}");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        if (!input.TryGetValue("imageText", out var imageText))
        {
            throw new ArgumentException("Image inference not provided in the input.");
        }

        if (!input.TryGetValue("imageDetails", out var imageDetails))
        {
            throw new ArgumentException("Image details not provided in the input.");
        }

        if (!input.TryGetValue("classifications", out var classifications))
        {
            throw new ArgumentException("classifications not provided in the input.");
        }

        try
        {
            var responseMetadata = await _s3Client.GetObjectMetadataAsync(_destinationBucket, key);

            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key)) ?? "";
            var content =
                "classifications:" + classifications.Trim() + "\n" +
                imageText.Trim() + "\n" + 
                imageDetails.Trim() + "\n";
            var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: ChunkSize);
            var splitText = textSplitter.SplitText(content);
            var textEmbeddings = new List<float[]>();
            var imageEmbeddings = new List<float[]>();

            var metadataKeys = responseMetadata.Metadata.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var metadata = responseMetadata.Metadata[metadataKeys.FirstOrDefault()!];
            context.Logger.LogInformation($"metadata: {metadata}");

            if (metadata.Equals("Text", StringComparison.OrdinalIgnoreCase))
            {
                textEmbeddings = await GetTextEmbeddingsAsync(context, content).ConfigureAwait(false);
            }

            if (metadata.Equals("Image", StringComparison.OrdinalIgnoreCase))
            {
                imageEmbeddings = await GetImageEmbeddingsAsync(context, memoryStream, contentType).ConfigureAwait(false);
            }

            if (metadata.Equals("TextAndImage", StringComparison.OrdinalIgnoreCase))
            {
                textEmbeddings = await GetTextEmbeddingsAsync(context, content).ConfigureAwait(false);
                imageEmbeddings = await GetImageEmbeddingsAsync(context, memoryStream, contentType).ConfigureAwait(false);
            }

            return new Dictionary<string, object>
            {
                { "key", key },
                { "textEmbeddings", textEmbeddings! },
                { "imageEmbeddings", imageEmbeddings! },
                { "classifications", classifications.Trim() },
                { "imageText", imageText.Trim() },
                { "imageDetails", imageDetails.Trim() },
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting embeddings: {e.Message}");
            throw;
        }
    }

    private static async Task<List<float[]>?> GetImageEmbeddingsAsync(ILambdaContext context, MemoryStream memoryStream, string contentType)
    {
        var imageEmbeddings = new List<float[]>();

        const string embeddingModelId = "amazon.titan-embed-image-v1";
        var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), embeddingModelId);

        var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);

        var embeddingsAsync = await embeddingModel.CreateEmbeddingsAsync("image", image);

        var embedding = embeddingsAsync?["embedding"]?.AsArray();
        if (embedding == null) return null;

        context.Logger.LogInformation($"GetImageEmbeddings got image embeddings");

        var f = new float[Dimensions];
        for (var j = 0; j < embedding.Count; j++)
        {
            f[j] = (float)embedding[j]?.AsValue()!;
        }

        imageEmbeddings.Add(f);

        return imageEmbeddings;
    }

    private static async Task<List<float[]>?> GetTextEmbeddingsAsync(ILambdaContext context, string content)
    {
        var textEmbeddings = new List<float[]>();

        const string textModelId = "amazon.titan-embed-text-v2:0";
        var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), textModelId);

        var embeddingsAsync = await embeddingModel.CreateEmbeddingsAsync(content);

        var embedding = embeddingsAsync?["embedding"]?.AsArray();
        if (embedding == null) return null!;

        context.Logger.LogInformation($"GetImageEmbeddings got text embeddings");

        var f = new float[Dimensions];
        for (var j = 0; j < embedding.Count; j++)
        {
            f[j] = (float)embedding[j]?.AsValue()!;
        }

        textEmbeddings.Add(f);

        return textEmbeddings;
    }
}