using System.Text.Json;

namespace Amazon.GenAI.ImageIngestionLambda;

public class Function
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly IAmazonBedrockRuntime _bedrockClient = new AmazonBedrockRuntimeClient();
    private readonly string _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");

    public async Task<Dictionary<string, string>> InferenceHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        if (!input.TryGetValue("key", out var imageKey))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        try
        {
            // Download the image from S3
            using var response = await _s3Client.GetObjectAsync(_destinationBucket, imageKey);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            // Convert image to base64
            var base64Image = Convert.ToBase64String(memoryStream.ToArray());

            // Prepare request for Claude
            var requestBody = new
            {
                prompt = $"Human: What's in this image?\n\nHuman: {{\"image\": \"{base64Image}\"}}\n\nAssistant: Certainly! I'll describe what I see in the image you've provided. ",
                max_tokens = 500,
                temperature = 0.5,
                top_p = 1,
                top_k = 250,
                anthropic_version = "bedrock-2023-05-31"
            };

            var invokeModelRequest = new InvokeModelRequest
            {
                ModelId = "anthropic.claude-3-sonnet-20240229-v1:0", // Use the appropriate model ID
                Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(requestBody))
            };

            // Invoke Bedrock model
            var bedrockResponse = await _bedrockClient.InvokeModelAsync(invokeModelRequest);

            // Parse the response
            using var responseStream = bedrockResponse.Body;
            using var reader = new StreamReader(responseStream);
            var jsonResponse = await reader.ReadToEndAsync();
            var responseObject = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);

            var inference = responseObject["completion"].GetString();

            context.Logger.LogInformation($"Bedrock inference: {inference}");

            return new Dictionary<string, string>
                {
                    { "inference", inference }
                };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting inference: {e.Message}");
            throw;
        }
    }
}