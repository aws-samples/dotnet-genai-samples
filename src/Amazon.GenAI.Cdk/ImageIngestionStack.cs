using Amazon.CDK.AWS.IAM;
using Amazon.CDK;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class ImageIngestionStack : Stack
{
    private readonly IStackConfiguration _config;

    public ImageIngestionStack(Construct scope, string id, IStackConfiguration config, StackProps props = null) 
        : base(scope, id, props)
    {
        _config = config;

        var s3Stack = new S3Stack(this, "S3Stack", _config);
        var dynamoDbStack = new DynamoDBStack(this, "DynamoDBStack", _config);
        var lambdaStack = new LambdaStack(this, "LambdaStack", _config, s3Stack, dynamoDbStack);
        var stepFunctionsStack = new StepFunctionsStack(this, "StepFunctionsStack", _config, lambdaStack, s3Stack);

        ConfigurePermissions(s3Stack, dynamoDbStack, lambdaStack, stepFunctionsStack);
        CreateOutputs(s3Stack, stepFunctionsStack);
    }

    private static void ConfigurePermissions(
        S3Stack s3Stack, 
        DynamoDBStack dynamoDbStack, 
        LambdaStack lambdaStack, 
        StepFunctionsStack stepFunctionsStack)
    {
        s3Stack.SourceBucket.GrantRead(lambdaStack.ImageResizerFunction);
        s3Stack.DestinationBucket.GrantWrite(lambdaStack.ImageResizerFunction);

        s3Stack.SourceBucket.GrantRead(lambdaStack.AddImageMetadataFunction);
        s3Stack.DestinationBucket.GrantWrite(lambdaStack.AddImageMetadataFunction);
        dynamoDbStack.Table.GrantWriteData(lambdaStack.AddImageMetadataFunction);

        s3Stack.DestinationBucket.GrantRead(lambdaStack.BedrockInferenceFunction);
        lambdaStack.BedrockInferenceFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "bedrock:InvokeModel" },
            Resources = new[] { "*" } // Restrict this to specific Bedrock model ARN in production
        }));

        stepFunctionsStack.StateMachine.GrantStartExecution(lambdaStack.S3EventHandlerFunction);
    }

    private void CreateOutputs(S3Stack s3Stack, StepFunctionsStack stepFunctionsStack)
    {
        new CfnOutput(this, "SourceBucketName", new CfnOutputProps { Value = s3Stack.SourceBucket.BucketName });
        new CfnOutput(this, "DestinationBucketName", new CfnOutputProps { Value = s3Stack.DestinationBucket.BucketName });
        new CfnOutput(this, "StateMachineArn", new CfnOutputProps { Value = stepFunctionsStack.StateMachine.StateMachineArn });
    }
}