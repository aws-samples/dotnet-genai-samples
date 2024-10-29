using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Amazon.GenAI.ImageIngestion;

public class ClassificationRetriever
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private const string TableName = "dotnet-genai-dynamo-table-539821354996";
    private const string ClassificationsAttribute = "classifications";
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true, PropertyNameCaseInsensitive = false };

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

        var jsonString = JsonSerializer.Serialize(uniqueClassifications, _options);
        Console.WriteLine(jsonString);

        return uniqueClassifications.ToList();
    }
}