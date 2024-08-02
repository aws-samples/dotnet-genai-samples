using System.Collections.Generic;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class StepFunctionsBuilder
{
    private readonly Construct _scope;
    private readonly IStackConfiguration _config;

    public StepFunctionsBuilder(Construct scope, IStackConfiguration config)
    {
        _scope = scope;
        _config = config;
    }

    public LambdaInvoke CreateImageResizeTask(Function imageResizerFunction)
    {
        var taskName = $"{_config.NamePrefix}-image-resize-task-{_config.NameSuffix}";
        return new LambdaInvoke(_scope, taskName, new LambdaInvokeProps
        {
            StateName = taskName,
            LambdaFunction = imageResizerFunction,
            PayloadResponseOnly = true,
            Payload = TaskInput.FromObject(new Dictionary<string, object>
            {
                { "bucket", JsonPath.StringAt("$.bucket") },
                { "key", JsonPath.StringAt("$.key") }
            }),
            ResultPath = "$.resizeResult"
        });
    }

    public LambdaInvoke CreateBedrockInferenceTask(Function bedrockInferenceFunction)
    {
        var taskName = $"{_config.NamePrefix}-bedrock-inference-task-{_config.NameSuffix}";
        return new LambdaInvoke(_scope, taskName, new LambdaInvokeProps
        {
            StateName = taskName,
            LambdaFunction = bedrockInferenceFunction,
            PayloadResponseOnly = true,
            Payload = TaskInput.FromObject(new Dictionary<string, object>
            {
                { "key", JsonPath.StringAt("$[0].resizeResult.key") }
            }),
            ResultPath = "$"
        });
    }

    public LambdaInvoke CreateAddImageMetadataTask(Function addImageMetadataFunction, Bucket destinationBucket)
    {
        var taskName = $"{_config.NamePrefix}-add-image-metadata-task-{_config.NameSuffix}";
        return new LambdaInvoke(_scope, taskName, new LambdaInvokeProps
        {
            StateName = taskName,
            LambdaFunction = addImageMetadataFunction,
            PayloadResponseOnly = true,
            Payload = TaskInput.FromObject(new Dictionary<string, object>
            {
                { "key", JsonPath.StringAt("$.key") },
                { "inference", JsonPath.StringAt("$.inference") },
                { "bucket", destinationBucket.BucketName }
            }),
            ResultPath = "$"
        });
    }

    public LambdaInvoke CreateGetImageEmbeddingsTask(Function getImageEmbeddingsFunction, Bucket destinationBucket)
    {
        var taskName = $"{_config.NamePrefix}-get-image-embeddings-task-{_config.NameSuffix}";
        return new LambdaInvoke(_scope, taskName, new LambdaInvokeProps
        {
            StateName = taskName,
            LambdaFunction = getImageEmbeddingsFunction,
            PayloadResponseOnly = true,
            Payload = TaskInput.FromObject(new Dictionary<string, object>
            {
                { "key", JsonPath.StringAt("$.key") },
                { "inference", JsonPath.StringAt("$.inference") },
                { "dynamoDbId", JsonPath.StringAt("$.dynamoDbId") },
                { "bucket", destinationBucket.BucketName }
            }),
            ResultPath = "$"
        });
    }

    public LambdaInvoke CreateAddDocumentToVectorDbTask(Function addImageMetadataFunction, Bucket destinationBucket)
    {
        var taskName = $"{_config.NamePrefix}-add-document-to-vectordb-task-{_config.NameSuffix}";
        return new LambdaInvoke(_scope, taskName, new LambdaInvokeProps
        {
            StateName = taskName,
            LambdaFunction = addImageMetadataFunction,
            PayloadResponseOnly = true,
            Payload = TaskInput.FromObject(new Dictionary<string, object>
            {
                { "key", JsonPath.StringAt("$.key") },
                { "bucket", destinationBucket.BucketName }
            }),
            ResultPath = "$"
        });
    }
}