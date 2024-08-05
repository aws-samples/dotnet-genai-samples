using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.BedrockRuntime;
using Amazon.GenAI.ImageIngestion.Abstractions;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageInference
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly AmazonBedrockRuntimeClient _bedrockClient = new AmazonBedrockRuntimeClient();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");


    public async Task<object> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageInference.  destination: {_destinationBucket}");

        const string prompt = "Provide a comprehensive description of this image, ensuring you cover every detail with meticulous attention to even the smallest elements.  Describe colors objects and size.  Search the internet for more background information.";

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        try
        {
            // Download the image from S3
            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key)) ?? "";
            var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);

            var textModelId = "anthropic.claude-3-haiku-20240307-v1:0";
            var textModel = new TextModel(_bedrockClient, textModelId);
            var generatedText = await textModel.GenerateAsync(prompt, image);

            context.Logger.LogInformation($"Bedrock inference: {generatedText}");

            return new Dictionary<string, string>
            {
                { "key", key },
                { "inference", generatedText }
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting inference: {e.Message}");
            throw;
        }
    }
}