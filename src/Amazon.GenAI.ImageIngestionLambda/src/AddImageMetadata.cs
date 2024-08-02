using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Core;

namespace Amazon.GenAI.ImageIngestionLambda;

public class AddImageMetadata
{
    public async Task<Dictionary<string, string?>> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        context.Logger.LogLine("in AddImageMetadata lambda");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {key}");

        if (!input.TryGetValue("bucket", out var bucket))
        {
            throw new ArgumentException("Image bucket not provided in the input.");
        }
        context.Logger.LogInformation($"bucket: {bucket}");

        if (!input.TryGetValue("inference", out var inference))
        {
            throw new ArgumentException("Image inference not provided in the input.");
        }
        context.Logger.LogInformation($"inference: {inference}");

        var client = new AmazonDynamoDBClient();

        var imageTable = Table.LoadTable(client, "dotnet-genai-images");
        var image = new Document
        {
            ["Id"] = Guid.NewGuid().ToString(),
            ["Key"] = key,
            ["Bucket"] = bucket,
            ["Text"] = inference,
            ["Score"] = 1.0
        };

        try
        {
            await imageTable.PutItemAsync(image);
            Console.WriteLine("Object saved successfully.");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving object: {ex.Message}");
        }

        return new Dictionary<string, string?>
        {
            { "dynamoDbId",  image["Id"] },
            { "key",  image["Key"] },
            { "bucket",  image["Bucket"] },
            { "inference", inference },
        };
    }
}