using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.BedrockRuntime;
using Amazon.DynamoDBv2;
using Amazon.GenAI.ImageIngestion.Abstractions;
using Amazon.Textract;
using Amazon.Textract.Model;
using Document = Amazon.Textract.Model.Document;
using OpenSearch.Client;

namespace Amazon.GenAI.ImageIngestion;

public class GetImageInference
{
    private readonly string? _namePrefix;
    private readonly string? _nameSuffix;
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly AmazonBedrockRuntimeClient _bedrockClient = new();
    private readonly AmazonDynamoDBClient _dynamoDbClient = new();
    private readonly IAmazonTextract _textractClient = new AmazonTextractClient();
    private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = false };
    private readonly int MaxLabels = 3;

    public GetImageInference()
    {
        _namePrefix = Environment.GetEnvironmentVariable("NAME_PREFIX");
        _nameSuffix = Environment.GetEnvironmentVariable("NAME_SUFFIX");
    }

    public async Task<Dictionary<string, string>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogInformation($"in GetImageInference. destination: {_destinationBucket}");

        // const string textModelId = "anthropic.claude-3-5-sonnet-20240620-v1:0";
        const string textModelId = "anthropic.claude-3-haiku-20240307-v1:0";
        var textModel = new TextModel(_bedrockClient, textModelId);
        var retriever = new ClassificationRetriever(_dynamoDbClient);

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        var image = await GetImageFromS3(key);
        var documentAnalysis = await GetDocumentAnalysis(key);
        var classifications = await retriever.GetUniqueClassificationsAsync();

        try
        {
            var prompt = CreatePrompt(MaxLabels, documentAnalysis, classifications);
            context.Logger.LogInformation($"prompt: {prompt}");

            var generatedText = await textModel.GenerateAsync(prompt, image).ConfigureAwait(false);
            context.Logger.LogInformation($"Bedrock inference: {generatedText}");

            var imageResponse = JsonSerializer.Deserialize<ImageResponse>(generatedText);
            var jsonString = JsonSerializer.Serialize(imageResponse, _options);
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

    private async Task<AnalysisResult> GetDocumentAnalysis(string key)
    {
        try
        {
            var request = new DetectDocumentTextRequest
            {
                Document = new Document
                {
                    S3Object = new Textract.Model.S3Object
                    {
                        Bucket = _destinationBucket,
                        Name = key
                    }
                }
            };
            var response = await _textractClient.DetectDocumentTextAsync(request);
            var documentAnalysis = new AnalysisResult
            {
                DetectedText = response.Blocks
                    .Where(b => b.BlockType == BlockType.LINE)
                    .Select(b => b.Text)
                    .ToList(),
                Confidence = response.Blocks
                    .Where(b => b.BlockType == BlockType.LINE)
                    .Select(b => b.Confidence)
                    .DefaultIfEmpty(0)
                    .Average()
            };

            var jsonString = JsonSerializer.Serialize(documentAnalysis, _options);
            Console.WriteLine(jsonString);

            return documentAnalysis;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task<BinaryData> GetImageFromS3(string key)
    {
        try
        {
            using var response = await _s3Client.GetObjectAsync(_destinationBucket, key).ConfigureAwait(false);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream).ConfigureAwait(false);
            memoryStream.Position = 0;

            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(key));
            var image = BinaryData.FromBytes(memoryStream.ToArray(), contentType);
            return image;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static string CreatePrompt(int maxLabels, AnalysisResult documentAnalysis, List<string> classifications)
    {
        return $@"
Analyze the uploaded image and provide a response in JSON format with the following elements:

1. Create a ""description"" field that provides a detailed, catalog-ready description of the image suitable for an e-commerce product listing. Include all key visual elements and features that would help sell the product.
2. Create a ""details"" field that captures:

All visible product information (brand names, product names, marketing text)
* Complete nutritional information if a nutrition label is present
* List of ingredients if visible
* Any additional product details shown in the image

Here's additional text about the image: 
{string.Join(',', documentAnalysis.DetectedText)}.

3. Create a ""classifications"" field containing 1-{maxLabels} classification labels that categorize the image.  A classification is 1-2 words.  First try to use these existing classifications if they apply:
{string.Join(',', classifications)}.

If none of these classifications fit, create new appropriate ones.  Remember, the classifications must be unique, not repeated in the response and there can only be a maximum of 3 classifications in the list.

4. Ensure the JSON response is valid and follows this structure:

Your response must be valid JSON following this structure, with all special characters properly escaped:
{{
  ""description"": ""string"",
  ""details"": ""string"",
  ""classifications"": [""string"", ""string"", ""string""]
}}

Important: Ensure all JSON strings properly escape:
- Double quotes with \\""
- Newlines with \\n
- Backslashes with \\\\
- Percent symbol % with %%
- All other JSON special characters
The final output must be valid parseable JSON.
";
    }
}