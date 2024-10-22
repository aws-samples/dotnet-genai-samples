using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.GenAI.ImageIngestion.Abstractions;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageInference
{
    private readonly string? _namePrefix;
    private readonly string? _nameSuffix;
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly AmazonBedrockRuntimeClient _bedrockClient = new();
    private readonly AmazonDynamoDBClient _dynamoDbClient = new();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");

    public GetImageInference()
    {
        _namePrefix = Environment.GetEnvironmentVariable("NAME_PREFIX");
        _nameSuffix = Environment.GetEnvironmentVariable("NAME_SUFFIX");
    }

    public async Task<Dictionary<string, string>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageInference. destination: {_destinationBucket}");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = false };

        try
        {
            var additionalInstruction = string.Empty;
            var retriever = new ClassificationRetriever(_dynamoDbClient);
            var uniqueClassifications = await retriever.GetUniqueClassificationsAsync();
            if (uniqueClassifications.Count > 0)
            {
                additionalInstruction = $@"here are a list of existing classifications: {string.Join(", ", uniqueClassifications)}.
use these classifications to describe the image in detail.  if the existing classifications is not applicable, then classify image.";
            }

            var uSerialize = JsonSerializer.Serialize(uniqueClassifications, options);
            context.Logger.LogInformation(uSerialize);

            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key).ConfigureAwait(false);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key));
            var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);

            const string textModelId = "anthropic.claude-3-haiku-20240307-v1:0";
            // const string textModelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";

            var textModel = new TextModel(_bedrockClient, textModelId);
            var prompt = CreatePrompt(3, additionalInstruction);
            var generatedText = await textModel.GenerateAsync(prompt, image).ConfigureAwait(false);

            context.Logger.LogInformation($"Bedrock inference: {generatedText}");

            var imageResponse = JsonSerializer.Deserialize<ImageResponse>(generatedText);
            var jsonString = JsonSerializer.Serialize(imageResponse, options);
            context.Logger.LogInformation(jsonString);

            if (imageResponse is not null)
            {
                var imageDetails = imageResponse.Details ?? string.Empty;
                var imageText = imageResponse.Description ?? string.Empty;

                return new Dictionary<string, string>
                {
                    { "classifications", imageResponse.Classifications?.Any() == true ? string.Join(", ", imageResponse.Classifications).ToLower() : string.Empty },
                    { "key", key },
                    { "imageText", imageText },
                    { "imageDetails", imageDetails },
                };
            }

            throw new InvalidOperationException("Failed to deserialize the image response.");
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting inference: {e.Message}");
            throw;
        }
    }

    public static string CreatePrompt(int maxLabels, string additionalInstruction)
    {
        return $@"
respond back in json format.
1. Provide a comprehensive description of this image.  this description will be used as the details for an online product catalog.
make this the 'description' parameter.
2. provide complete detail information from the product image.  
if the product has a nutritional label, then describe as much information about the nutritional facts.
this information will be used to search. make this the 'details' parameter.
3. classify this image. classify image using 1-2 words. return 1-{maxLabels} labels. make this the 'classifications' parameters.
{additionalInstruction}
";
    }
}

public class ClassificationRetriever
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private const string TableName = "dotnet-genai-dynamo-table-539821354996";
    private const string ClassificationsAttribute = "classifications";

    public ClassificationRetriever(IAmazonDynamoDB dynamoDbClient)
    {
        _dynamoDbClient = dynamoDbClient;
    }

    public async Task<List<string>> GetUniqueClassificationsAsync()
    {
        var request = new ScanRequest
        {
            TableName = TableName,
            ProjectionExpression = ClassificationsAttribute
        };

        var uniqueClassifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        do
        {
            var response = await _dynamoDbClient.ScanAsync(request);

            foreach (var item in response.Items)
            {
                if (!item.TryGetValue(ClassificationsAttribute, out var classificationsAttribute) ||
                    classificationsAttribute.S is not { } classifications) continue;

                var splitClassifications = classifications.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim());
                foreach (var classification in splitClassifications)
                {
                    uniqueClassifications.Add(classification.ToLower());
                }
            }

            request.ExclusiveStartKey = response.LastEvaluatedKey;
        } while (request.ExclusiveStartKey is { Count: > 0 });

        return uniqueClassifications.ToList();
    }
}

public class ImageResponse
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("classifications")]
    public List<string>? Classifications { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}
