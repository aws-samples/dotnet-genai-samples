using System;
using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

namespace Amazon.GenAI.ImageIngestionLambda;

public class S3EventHandler
{
    private readonly IAmazonStepFunctions _stepFunctionsClient;
    private readonly string? _stateMachineArn;

    public S3EventHandler()
    {
        _stepFunctionsClient = new AmazonStepFunctionsClient();
        _stateMachineArn = Environment.GetEnvironmentVariable("stateMachineArn");
    }

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        context.Logger.LogLine("in S3EventHandler lambda");
        context.Logger.LogLine($"_stateMachineArn: {_stateMachineArn}");

        var s3EventRecords = evnt.Records.Select(record => new
        {
            bucket = record.S3.Bucket.Name,
            key = record.S3.Object.Key
        }).ToList();

        var input = JsonSerializer.Serialize(new { s3EventRecords });

        context.Logger.LogLine($"input: {input}");

        var startExecutionRequest = new StartExecutionRequest
        {
            StateMachineArn = _stateMachineArn,
            Input = input
        };

        await _stepFunctionsClient.StartExecutionAsync(startExecutionRequest);
    }
}