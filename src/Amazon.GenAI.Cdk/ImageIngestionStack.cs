using System.Collections.Generic;

namespace Amazon.GenAI.Cdk;

using CDK;
using CDK.AWS.Lambda;
using CDK.AWS.Lambda.EventSources;
using CDK.AWS.S3;
using CDK.AWS.StepFunctions;
using CDK.AWS.StepFunctions.Tasks;
using Constructs;

public class ImageIngestionStack : Stack
{
    public ImageIngestionStack(Construct scope, string id, ImageIngestionStackProps props = null) : base(scope, id, props)
    {
        // Create S3 buckets
        var sourceBucketName = $"{props?.AppProps.NamePrefix}-source-{props.AppProps.NameSuffix}";
        var sourceBucket = new Bucket(this, sourceBucketName, new BucketProps
        {
            BucketName = sourceBucketName,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });

        var destinationBucketName = $"{props.AppProps.NamePrefix}-destination-{props.AppProps.NameSuffix}";
        var destinationBucket = new Bucket(this, destinationBucketName, new BucketProps
        {
            BucketName = destinationBucketName,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });

        // Create S3 event handler Lambda function without the state machine ARN
        var s3EventHandlerFunctionName = $"{props.AppProps.NamePrefix}-s3-event-{props.AppProps.NameSuffix}";
        var s3EventFunctionHandler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.S3TriggerFunction::FunctionHandler";
        var s3EventHandlerFunction = new Function(this, s3EventHandlerFunctionName, new FunctionProps
        {
            FunctionName = s3EventHandlerFunctionName,
            Runtime = Runtime.DOTNET_6,
            Handler = s3EventFunctionHandler,
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net6.0"),
            Timeout = Duration.Seconds(30),
            MemorySize = 512
        });

        // Add S3 event source to S3EventHandlerFunction
        s3EventHandlerFunction.AddEventSource(new S3EventSource(sourceBucket, new S3EventSourceProps
        {
            Events = new[] { EventType.OBJECT_CREATED }
        }));

        //// Create image ingestion Lambda function
        //var imageIngestionFunctionName = $"{props.AppProps.NamePrefix}-image-ingestion-handler-{props.AppProps.NameSuffix}";
        //var imageIngestionFunctionHandler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageResizer::FunctionHandler";
        //var imageIngestionFunction = new Function(this, imageIngestionFunctionName, new FunctionProps
        //{
        //    FunctionName = imageIngestionFunctionName,
        //    Runtime = Runtime.DOTNET_8,
        //    Handler = imageIngestionFunctionHandler,
        //    Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion"),
        //    Timeout = Duration.Minutes(5),
        //    MemorySize = 1024,
        //    Environment = new Dictionary<string, string>
        //    {
        //        { "destinationBucketName", destinationBucket.BucketName }
        //    }
        //});

        //// Grant permissions
        //sourceBucket.GrantRead(imageIngestionFunction);
        //destinationBucket.GrantWrite(imageIngestionFunction);

        //// Create Step Functions state machine
        //var mapStateName = $"{props.AppProps.NamePrefix}-map-state-{props.AppProps.NameSuffix}";
        //var mapState = new Map(this, mapStateName, new MapProps
        //{
        //    MaxConcurrency = 10,
        //    ItemsPath = JsonPath.StringAt("$.s3EventRecords")
        //});

        //var imageIngestTaskName = $"{props.AppProps.NamePrefix}-image-ingestion-task-{props.AppProps.NameSuffix}";
        //var imageIngestTask = new LambdaInvoke(this, imageIngestTaskName, new LambdaInvokeProps
        //{
        //    LambdaFunction = imageIngestionFunction,
        //    PayloadResponseOnly = true,
        //    Payload = TaskInput.FromObject(new Dictionary<string, object>
        //    {
        //        { "bucket", JsonPath.StringAt("$.bucket") },
        //        { "key", JsonPath.StringAt("$.key") }
        //    })
        //});

        //mapState.Iterator(imageIngestTask);

        //var stateMachine = new StateMachine(this, stateMachineName, new StateMachineProps
        //{
        //    Definition = mapState
        //});

        // Grant Step Functions permission to invoke Lambda
        //    stateMachine.GrantTaskResponse(imageIngestionFunction);



        //// Create the state machine
        //var stateMachineName = $"{props.AppProps.NamePrefix}-image-ingestion-state-machine-{props.AppProps.NameSuffix}";
        //var stateMachine = new StateMachine(this, stateMachineName, new StateMachineProps
        //{
        //    StateMachineName = stateMachineName,
        //    Definition = mapState
        //});

        //// Add the state machine ARN as an environment variable after creating the state machine
        //s3EventHandlerFunction.AddEnvironment("stateMachineArn", stateMachine.StateMachineArn);



        //// Grant S3EventHandlerFunction permission to start Step Functions execution
        //stateMachine.GrantStartExecution(s3EventHandlerFunction);

        //// Output the bucket names and state machine ARN
        //new CfnOutput(this, "SourceBucketName", new CfnOutputProps { Value = sourceBucket.BucketName });
        //new CfnOutput(this, "DestinationBucketName", new CfnOutputProps { Value = destinationBucket.BucketName });
        ////  new CfnOutput(this, "StateMachineArn", new CfnOutputProps { Value = stateMachine.StateMachineArn });
    }
}



//public class ImageIngestionStack : Stack
//{
//    public ImageIngestionStack(Construct scope, string id, ImageIngestionStackProps props = null) : base(scope, id, props)
//    {
//        //// Create S3 buckets
//        //var sourceBucketName = $"{props?.AppProps.NamePrefix}-source-bucket-{props?.AppProps.NameSuffix}";
//        //var sourceBucket = new Bucket(this, sourceBucketName, new BucketProps
//        //{
//        //    BucketName = sourceBucketName,
//        //    Versioned = true,
//        //    RemovalPolicy = RemovalPolicy.DESTROY,
//        //    AutoDeleteObjects = true
//        //});

//        //var destinationBucketName = $"{props?.AppProps.NamePrefix}-destination-bucket-{props?.AppProps.NameSuffix}";
//        //var destinationBucket = new Bucket(this, destinationBucketName, new BucketProps
//        //{
//        //    BucketName = destinationBucketName,
//        //    Versioned = true,
//        //    RemovalPolicy = RemovalPolicy.DESTROY,
//        //    AutoDeleteObjects = true
//        //});

//        //// Create Lambda function for image resizing
//        //var resizeFunctionName = $"{props?.AppProps.NamePrefix}-resize-function-{props?.AppProps.NameSuffix}";
//        //var resizeFunction = new Function(this, resizeFunctionName, new FunctionProps
//        //{
//        //    FunctionName = resizeFunctionName,
//        //    Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
//        //    Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.ImageResizer::FunctionHandler",
//        //    Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion"),
//        //    Timeout = Duration.Minutes(5),
//        //    MemorySize = 1024,
//        //    Environment = new Dictionary<string, string>
//        //    {
//        //        { "DESTINATION_BUCKET", destinationBucket.BucketName }
//        //    }
//        //});

//        //// Grant Lambda permissions
//        //sourceBucket.GrantRead(resizeFunction);
//        //destinationBucket.GrantWrite(resizeFunction);

//        //// Define Step Function tasks
//        //var resizeTaskName = $"{props?.AppProps.NamePrefix}-resize-task-{props?.AppProps.NameSuffix}";
//        //var resizeTask = new LambdaInvoke(this, resizeTaskName, new LambdaInvokeProps
//        //{
//        //    StateName = resizeTaskName,
//        //    LambdaFunction = resizeFunction,
//        //    PayloadResponseOnly = true
//        //});

//        //var startExecutionTaskName = $"{props?.AppProps.NamePrefix}-start-execution-{props?.AppProps.NameSuffix}";
//        //var startExecution = new Pass(this, startExecutionTaskName, new PassProps
//        //{
//        //    StateName = startExecutionTaskName,
//        //    Result = Result.FromObject(new Dictionary<string, object>
//        //    {
//        //        { "message", "Starting image processing" }
//        //    }),
//        //    ResultPath = "$.startInfo"
//        //});

//        //var endExecutionTaskName = $"{props?.AppProps.NamePrefix}-end-execution-{props?.AppProps.NameSuffix}";
//        //var processingComplete = new Pass(this, endExecutionTaskName, new PassProps
//        //{
//        //    StateName = endExecutionTaskName,
//        //    Result = Result.FromObject(new Dictionary<string, object>
//        //    {
//        //        { "message", "Image processing completed" }
//        //    }),
//        //    ResultPath = "$.completeInfo"
//        //});

//        //// Create Step Functions state machine
//        //var mapState = new Map(this, "MapState", new MapProps
//        //{
//        //    MaxConcurrency = 10,
//        //    ItemsPath = JsonPath.StringAt("$.s3EventRecords")
//        //});

//        //var resizeTask = new LambdaInvoke(this, "ResizeTask", new LambdaInvokeProps
//        //{
//        //    LambdaFunction = imageResizerFunction,
//        //    PayloadResponseOnly = true,
//        //    Payload = TaskInput.FromObject(new Dictionary<string, object>
//        //    {
//        //        { "bucket", JsonPath.StringAt("$.bucket") },
//        //        { "key", JsonPath.StringAt("$.key") }
//        //    })
//        //});

//        //mapState.Iterator(resizeTask);

//        //var stateMachine = new StateMachine(this, "ImageResizerStateMachine", new StateMachineProps
//        //{
//        //    Definition = mapState
//        //});

//        // Grant Step Functions permission to invoke Lambda
//        // stateMachine.GrantTaskResponse(resizeFunction);


//        //// Chain the tasks
//        //var definition = DefinitionBody.FromChainable(
//        //    startExecution
//        //        .Next(resizeTask)
//        //        .Next(processingComplete)
//        //);

//        //// Create the State Machine
//        //var stateMachineName = $"{props?.AppProps.NamePrefix}-image-ingestion-state-machine-{props?.AppProps.NameSuffix}";
//        //var stateMachine = new StateMachine(this, stateMachineName, new StateMachineProps
//        //{
//        //    StateMachineName = stateMachineName,
//        //    DefinitionBody = definition,
//        //    StateMachineType = StateMachineType.EXPRESS
//        //});

//        //// Grant Step Function permissions to invoke Lambda
//        //stateMachine.GrantTaskResponse(resizeFunction);

//        //// Create S3 notification to trigger Step Function
//        //var s3TriggerStepFunctionName = $"{props?.AppProps.NamePrefix}-s3-trigger-step-function-{props?.AppProps.NameSuffix}";
//        //sourceBucket.AddEventNotification(EventType.OBJECT_CREATED,
//        //    new LambdaDestination(new Function(this, s3TriggerStepFunctionName, new FunctionProps
//        //    {
//        //        FunctionName = s3TriggerStepFunctionName,
//        //        Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
//        //        Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.S3TriggerFunction::FunctionHandler",
//        //        Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion"),
//        //        Environment = new Dictionary<string, string>
//        //        {
//        //            { "STATE_MACHINE_ARN", stateMachine.StateMachineArn },
//        //        }
//        //    }))
//        //);

//        //// Grant permission to trigger Step Function
//        //stateMachine.GrantStartExecution(new ServicePrincipal("lambda.amazonaws.com"));
//    }
//}