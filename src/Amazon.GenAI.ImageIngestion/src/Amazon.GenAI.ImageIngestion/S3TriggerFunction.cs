using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Amazon.GenAI.ImageIngestion;

public class S3TriggerFunction
{
    private readonly IAmazonStepFunctions _stepFunctionsClient;
    private readonly string? _stateMachineArn;

    public S3TriggerFunction()
    {
        _stepFunctionsClient = new AmazonStepFunctionsClient();
        _stateMachineArn = Environment.GetEnvironmentVariable("STATE_MACHINE_ARN");
    }

    public async Task<string> FunctionHandler(S3Event s3Event, ILambdaContext context)
    {
        context.Logger.LogInformation($"in S3TriggerFunction");

        //if (s3Event.Records == null || s3Event.Records.Count == 0)
        //{
        //    context.Logger.LogWarning("No S3 event records received.");
        //    return "No S3 event records received.";
        //}

        //var tasks = s3Event.Records.Select(record => ProcessRecordAsync(record, context));
      //  await Task.WhenAll(tasks);

        return "Step Function executions started successfully.";
    }

    private async Task ProcessRecordAsync(S3Event.S3EventNotificationRecord record, ILambdaContext context)
    {
        var s3 = record.S3;
        var bucketName = s3.Bucket.Name;
        var objectKey = s3.Object.Key;

        context.Logger.LogInformation($"Processing object: {objectKey} in bucket: {bucketName}");

        var input = new
        {
            bucket = bucketName,
            key = objectKey
        };

        var startExecutionRequest = new StartExecutionRequest
        {
            StateMachineArn = _stateMachineArn,
            Input = JsonSerializer.Serialize(input)
        };

        try
        {
            var response = await _stepFunctionsClient.StartExecutionAsync(startExecutionRequest);
            context.Logger.LogInformation($"Step Function execution started. Execution ARN: {response.ExecutionArn}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error starting Step Function execution: {ex.Message}");
            throw;
        }
    }
}