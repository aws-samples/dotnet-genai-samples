using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class LambdaStack : Construct
{
    public Function S3EventHandlerFunction { get; }
    public Function ImageResizerFunction { get; }
    public Function AddImageMetadataFunction { get; }
    public Function BedrockInferenceFunction { get; }
    public Function AddDocumentFunction { get; }
    public Function GetImageEmbeddingsFunction { get; set; }


    public LambdaStack(Construct scope, string id, IStackConfiguration config, S3Stack s3Stack, DynamoDBStack dynamoDbStack) : base(scope, id)
    {
        S3EventHandlerFunction = CreateS3EventHandlerFunction(config);
        ImageResizerFunction = CreateImageResizerFunction(config, s3Stack.DestinationBucket);
        AddImageMetadataFunction = CreateAddImageMetadataFunction(config, s3Stack.DestinationBucket);
        BedrockInferenceFunction = CreateBedrockInferenceFunction(config, s3Stack.DestinationBucket);
        AddDocumentFunction = CreateAddDocumentToVectorDbFunction(config, s3Stack.DestinationBucket);
        GetImageEmbeddingsFunction = CreateGetImageEmbeddingsFunction(config, s3Stack.DestinationBucket);

        S3EventHandlerFunction.AddEventSource(new S3EventSource(s3Stack.SourceBucket, new S3EventSourceProps
        {
            Events = new[] { EventType.OBJECT_CREATED }
        }));
    }

    private Function CreateS3EventHandlerFunction(IStackConfiguration config)
    {
        var functionName = $"{config.NamePrefix}-s3-event-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.S3EventHandler::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Seconds(30),
            MemorySize = 512
        });
    }

    private Function CreateImageResizerFunction(IStackConfiguration config, Bucket destinationBucket)
    {
        var functionName = $"{config.NamePrefix}-image-resizer-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.ImageResizer::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "destinationBucketName", destinationBucket.BucketName }
            }
        });
    }

    private Function CreateAddImageMetadataFunction(IStackConfiguration config, Bucket destinationBucket)
    {
        var functionName = $"{config.NamePrefix}-add-image-metadata-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.AddImageMetadata::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "destinationBucketName", destinationBucket.BucketName }
            }
        });
    }

    private Function CreateBedrockInferenceFunction(IStackConfiguration config, Bucket destinationBucket)
    {
        var functionName = $"{config.NamePrefix}-add-bedrock-inference-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.BedrockInference::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "destinationBucketName", destinationBucket.BucketName }
            }
        });
    }

    private Function CreateGetImageEmbeddingsFunction(IStackConfiguration config, Bucket destinationBucket)
    {
        var functionName = $"{config.NamePrefix}-get-image-embeddings-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.GetImageEmbeddings::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "destinationBucketName", destinationBucket.BucketName }
            }
        });
    }

    private Function CreateAddDocumentToVectorDbFunction(IStackConfiguration config, Bucket destinationBucket)
    {
        var functionName = $"{config.NamePrefix}-add-document-to-vectordb-{config.NameSuffix}";
        return new Function(this, functionName, new FunctionProps
        {
            FunctionName = functionName,
            Runtime = Amazon.CDK.AWS.Lambda.Runtime.DOTNET_6,
            Handler = "Amazon.GenAI.ImageIngestionLambda::Amazon.GenAI.ImageIngestionLambda.AddToVectorDb::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestionLambda/src", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "destinationBucketName", destinationBucket.BucketName }
            }
        });
    }
}