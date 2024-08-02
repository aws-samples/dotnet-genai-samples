using Amazon.CDK.AWS.StepFunctions;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class StepFunctionsStack : Construct
{
    public StateMachine StateMachine { get; }

    public StepFunctionsStack(Construct scope, string id, IStackConfiguration config, LambdaStack lambdaStack, S3Stack s3Stack) : base(scope, id)
    {
        var builder = new StepFunctionsBuilder(this, config);

        var imageResizeTask = builder.CreateImageResizeTask(lambdaStack.ImageResizerFunction);
        var bedrockInferenceTask = builder.CreateBedrockInferenceTask(lambdaStack.BedrockInferenceFunction);
        var addImageMetadataTask = builder.CreateAddImageMetadataTask(lambdaStack.AddImageMetadataFunction, s3Stack.DestinationBucket);
        var addDocumentToVectorDbTask =
            builder.CreateAddDocumentToVectorDbTask(lambdaStack.AddDocumentFunction, s3Stack.DestinationBucket);
        var getImageEmbeddingsTask =
            builder.CreateGetImageEmbeddingsTask(lambdaStack.GetImageEmbeddingsFunction, s3Stack.DestinationBucket);

        var mapState = new Map(this, $"{config.NamePrefix}-map-state-{config.NameSuffix}", new MapProps
        {
            StateName = $"{config.NamePrefix}-map-state-{config.NameSuffix}",
            MaxConcurrency = 10,
            ItemsPath = JsonPath.StringAt("$.s3EventRecords"),
        });

        mapState.Iterator(imageResizeTask)
            .Next(bedrockInferenceTask)
            .Next(addImageMetadataTask)
            .Next(getImageEmbeddingsTask)
            .Next(addDocumentToVectorDbTask)
            ;

        StateMachine = new StateMachine(this, $"{config.NamePrefix}-image-ingestion-state-machine-{config.NameSuffix}", new StateMachineProps
        {
            StateMachineName = $"{config.NamePrefix}-image-ingestion-state-machine-{config.NameSuffix}",
            Definition = mapState
        });

        lambdaStack.S3EventHandlerFunction.AddEnvironment("stateMachineArn", StateMachine.StateMachineArn);
    }
}