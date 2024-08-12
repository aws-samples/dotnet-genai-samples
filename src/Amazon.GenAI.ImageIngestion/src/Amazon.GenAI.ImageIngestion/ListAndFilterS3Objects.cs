using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;

namespace Amazon.GenAI.ImageIngestion;

public class ListAndFilterS3Objects
{
    private readonly IAmazonS3 _s3Client = new AmazonS3Client();
    private readonly string? _stateMachineArn = Environment.GetEnvironmentVariable("STATE_MACHINE_ARN");
    private readonly string? _tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME");
    private readonly IAmazonStepFunctions _stepFunctionsClient = new AmazonStepFunctionsClient();
    private readonly AmazonDynamoDBClient _dynamoDbClient = new AmazonDynamoDBClient();

    public async Task FunctionHandler(CloudWatchEvent<ProcessExistingBucketDetail> @event, ILambdaContext context)
    {
        context.Logger.LogInformation($"###  in ListAndFilterS3Objects ------------");

        var bucketName = @event.Detail.BucketName;
        var request = new ListObjectsV2Request
        {
            BucketName = bucketName
        };

        context.Logger.LogInformation($"got bucketName: {bucketName}");

        try
        {
            do
            {
                var response = await _s3Client.ListObjectsV2Async(request);

                context.Logger.LogInformation($"got files: {response.S3Objects.Count}");

                foreach (var s3Object in response.S3Objects)
                {
                    var exists = await DoesKeyExistInTable(s3Object.Key, bucketName);

                    if (exists == false && (s3Object.Key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                               s3Object.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                    {
                        await StartStateMachineExecution(bucketName, s3Object.Key);
                    }
                }

                request.ContinuationToken = response.NextContinuationToken;
            } while (!string.IsNullOrEmpty(request.ContinuationToken));
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error getting inference: {e.Message}");
            throw;
        }
    }

    private async Task<bool> DoesKeyExistInTable(string key, string bucketName)
    {
        var table = Table.LoadTable(_dynamoDbClient, _tableName);

        var find = new Dictionary<string, DynamoDBEntry>
        {
            { "key", key },
            { "bucketName", bucketName }
        };

        var document = await table.GetItemAsync(find);

        return document != null;
    }

    private async Task StartStateMachineExecution(string bucketName, string key)
    {
        var input = new StateMachineStartObj
        {
            Detail = new StateMachineDetail
            {
                Bucket = new StateMachineBucket { Name = bucketName },
                Object = new StateMachineObject { Key = key }
            },
        };

        var request = new StartExecutionRequest
        {
            StateMachineArn = _stateMachineArn,
            Input = JsonSerializer.Serialize(input)
        };

        await _stepFunctionsClient.StartExecutionAsync(request);
    }
}

public class ProcessExistingBucketDetail(string bucketName)
{
    public string BucketName { get; set; } = bucketName;
}

public class StateMachineStartObj
{
    [JsonPropertyName("detail")]
    public StateMachineDetail Detail { get; set; } = new();
}

public class StateMachineDetail
{
    [JsonPropertyName("bucket")]
    public StateMachineBucket Bucket { get; set; } = new();
    [JsonPropertyName("object")]
    public StateMachineObject Object { get; set; } = new();
}

public class StateMachineBucket
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public class StateMachineObject
{
    [JsonPropertyName("key")]
    public string Key { get; set; }
}