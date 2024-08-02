using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.GenAI.ImageIngestionLambda.Abstractions;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.Util;

namespace Amazon.GenAI.ImageIngestionLambda;

public class BedrockInference
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly IAmazonBedrockRuntime _bedrockClient = new AmazonBedrockRuntimeClient();
    private readonly string _destinationBucket = Environment.GetEnvironmentVariable("destinationBucketName");

    public async Task<Dictionary<string, string>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in BedrockInference");

        if (!input.TryGetValue("key", out var imageKey))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {imageKey}");
        context.Logger.LogInformation($"_destinationBucket: {_destinationBucket}");

        var modelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";

        try
        {
            // Download the image from S3
            using var s3Response = await _s3Client.GetObjectAsync(_destinationBucket, imageKey);
            using var memoryStream = new MemoryStream();
            await s3Response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var image = BinaryData.FromBytes(memoryStream.ToArray(), "image/jpeg");
            var prompt = "Provide a comprehensive description of this image, ensuring you cover every detail with meticulous attention to even the smallest elements.  Search the internet for more background information.";
            var bodyJson = AnthropicClaude3.CreateBodyJson(prompt, image);

            var bedrockResponse = await new AmazonBedrockRuntimeClient().InvokeModelAsync(modelId, bodyJson).ConfigureAwait(false);
            var generatedText = bedrockResponse?["content"]?[0]?["text"]?.GetValue<string>() ?? "";

            context.Logger.LogInformation($"Bedrock inference: {generatedText}");

            return new Dictionary<string, string>
            {
                { "key", imageKey },
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