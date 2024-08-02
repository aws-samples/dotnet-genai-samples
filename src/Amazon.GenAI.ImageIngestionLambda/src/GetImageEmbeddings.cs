using Amazon.BedrockRuntime;
using Amazon.GenAI.ImageIngestionLambda.Abstractions;
using Amazon.Lambda.Core;
using Amazon.S3;
using OpenSearch.Client;

namespace Amazon.GenAI.ImageIngestionLambda;

public class GetImageEmbeddings
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly IAmazonBedrockRuntime _bedrockClient = new AmazonBedrockRuntimeClient();
    private readonly string _destinationBucket = Environment.GetEnvironmentVariable("destinationBucketName");

    public async Task<Dictionary<string, string>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageEmbeddings");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {key}");

        if (!input.TryGetValue("inference", out var inference))
        {
            throw new ArgumentException("Image inference not provided in the input.");
        }
        context.Logger.LogInformation($"inference: {inference}");


        context.Logger.LogInformation($"_destinationBucket: {_destinationBucket}");

        var modelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";

        try
        {
            using var s3Response = await _s3Client.GetObjectAsync(_destinationBucket, key);
            using var memoryStream = new MemoryStream();
            await s3Response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

           // BinaryData image = BinaryData.FromBytes(memoryStream.ToArray(), "image/jpeg");

            var embeddingModelId = "amazon.titan-embed-image-v1";
            var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), embeddingModelId);

            //var embeddingsAsync = await embeddingModel.CreateEmbeddingsAsync(inference, image);

            //var content = document.PageContent.Trim();
            //var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: chuckSize);
            //var splitText = textSplitter.SplitText(content);
            //var embeddings = new List<float[]>(capacity: splitText.Count);
            //var bytes = Convert.FromBase64String((document.Metadata["base64"] as string)!);
            //var image = BinaryData.FromBytes(bytes);
            //var embeddingTasks = splitText.Select(text => embeddingModel.CreateEmbeddingsAsync(document.PageContent, image))
            //    .ToList();
            //var results = await Task.WhenAll(embeddingTasks).ConfigureAwait(false);

            //foreach (var response in results)
            //{
            //    var embedding = response?["embedding"]?.AsArray();
            //    if (embedding == null) continue;

            //    var f = new float[_options.Dimensions!.Value];
            //    for (var j = 0; j < embedding.Count; j++)
            //    {
            //        f[j] = (float)embedding[j]?.AsValue()!;
            //    }

            //    embeddings.Add(f);
            //}

            //var vectorRecord = new VectorRecord
            //{
            //    Text = document.PageContent,
            //    Path = document.Metadata["path"] as string,
            //    Base64 = document.Metadata["base64"] as string,
            //    Vector = embeddings.ToArray().SelectMany(x => x).ToArray()
            //};

            //bulkDescriptor.Index<VectorRecord>(desc => desc
            //    .Document(vectorRecord)
            //    .Index(_indexName)
            //);

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