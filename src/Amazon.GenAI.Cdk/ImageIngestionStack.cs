using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class ImageIngestionStack : Stack
{
    public ImageIngestionStack(Construct scope, string id, ImageIngestionStackProps props = null) : base(scope, id, props)
    {
        // Define S3 buckets
        var sourceBucketName = $"{props?.AppProps.NamePrefix}-source-{props.AppProps.NameSuffix}";
        var sourceBucket = new Bucket(this, sourceBucketName, new BucketProps
        {
            BucketName = sourceBucketName,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true,
            EventBridgeEnabled = true
        });

        var destinationBucketName = $"{props.AppProps.NamePrefix}-destination-{props.AppProps.NameSuffix}";
        var destinationBucket = new Bucket(this, destinationBucketName, new BucketProps
        {
            BucketName = destinationBucketName,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });

        // Define Lambda functions
        var imageResizerFunctionName = $"{props?.AppProps.NamePrefix}-image-resizer-function-{props?.AppProps.NameSuffix}";
        var imageResizerFunction = new Function(this, imageResizerFunctionName, new FunctionProps
        {
            FunctionName = imageResizerFunctionName,
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.ImageResizer::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net8.0"),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "DESTINATION_BUCKET", destinationBucket.BucketName }
            },
            Role = new Role(this, $"{imageResizerFunctionName}-role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3FullAccess")
                }
            })
        });

        var getImageInferenceFunctionName = $"{props?.AppProps.NamePrefix}-get-image-inference-function-{props?.AppProps.NameSuffix}";
        var getImageInferenceFunction = new Function(this, getImageInferenceFunctionName, new FunctionProps
        {
            FunctionName = getImageInferenceFunctionName,
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.GetImageInference::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net8.0"),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "DESTINATION_BUCKET", destinationBucket.BucketName }
            },
            Role = new Role(this, $"{getImageInferenceFunctionName}-role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
                }
            })
        });
        getImageInferenceFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "bedrock:InvokeModel" },
            Resources = new[] { "*" } // Restrict this to specific Bedrock model ARN in production
        }));

        var getImageEmbeddingsFunctionName = $"{props?.AppProps.NamePrefix}-get-image-embeddings-function-{props?.AppProps.NameSuffix}";
        var getImageEmbeddingsFunction = new Function(this, getImageEmbeddingsFunctionName, new FunctionProps
        {
            FunctionName = getImageEmbeddingsFunctionName,
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.GetImageEmbeddings::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net8.0"),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "DESTINATION_BUCKET", destinationBucket.BucketName }
            },
            Role = new Role(this, $"{getImageEmbeddingsFunctionName}-role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
                }
            })
        });
        getImageEmbeddingsFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "bedrock:InvokeModel" },
            Resources = new[] { "*" } // Restrict this to specific Bedrock model ARN in production
        }));

        var addToOpenSearchFunctionName = $"{props?.AppProps.NamePrefix}-add-to-opensearch-function-{props?.AppProps.NameSuffix}";
        var addToOpenSearchFunction = new Function(this, addToOpenSearchFunctionName, new FunctionProps
        {
            FunctionName = addToOpenSearchFunctionName,
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.AddToOpenSearch::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net8.0"),
            Timeout = Duration.Minutes(5),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
                { "DESTINATION_BUCKET", destinationBucket.BucketName }
            },
            Role = new Role(this, $"{addToOpenSearchFunctionName}-role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                }
            })
        });
        getImageEmbeddingsFunction.AddToRolePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "bedrock:InvokeModel" },
            Resources = new[] { "*" } // Restrict this to specific Bedrock model ARN in production
        }));

        // Define DynamoDB table
        var tableName = $"{props?.AppProps.NamePrefix}-results-table-{props?.AppProps.NameSuffix}";
        var table = new Table(this, tableName, new TableProps
        {
            TableName = tableName,
            PartitionKey = new Attribute { Name = "key", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });

        // Define Step Functions tasks
        var filterS3Json = new Pass(this, "FilterS3Json", new PassProps
        {
            Parameters = new Dictionary<string, object>
            {
                ["bucketName.$"] = "$.detail.bucket.name",
                ["key.$"] = "$.detail.object.key",
            }
        });

        var invokeImageResizer = new LambdaInvoke(this, "ImageResizer", new LambdaInvokeProps
        {
            LambdaFunction = imageResizerFunction,
            OutputPath = "$.Payload",
        });

        var invokeGetImageInference = new LambdaInvoke(this, "GetImageInference", new LambdaInvokeProps
        {
            LambdaFunction = getImageInferenceFunction,
        });

        var invokeGetImageEmbeddings = new LambdaInvoke(this, "GetImageEmbeddings", new LambdaInvokeProps
        {
            LambdaFunction = getImageEmbeddingsFunction,
            InputPath = "$.Payload"
        });

        var transformResults = new Pass(this, "TransformInferenceEmbeddingsResults", new PassProps
        {
            Parameters = new Dictionary<string, object>
            {
                ["key.$"] = "$.Payload.key",
                ["inference.$"] = "$.Payload.inference",
              //  ["embeddings.$"] = "$.Payload.embeddings"
            }
        });

        var putItem = new DynamoPutItem(this, "PutItem", new DynamoPutItemProps
        {
            Table = table,
            Item = new Dictionary<string, DynamoAttributeValue>
            {
                ["key"] = DynamoAttributeValue.FromString(JsonPath.StringAt("$.key")),
                ["bucketName"] = DynamoAttributeValue.FromString(destinationBucketName),
                ["inference"] = DynamoAttributeValue.FromString(JsonPath.StringAt("$.inference")),
            },
            ResultSelector = new Dictionary<string, object>
            {
                ["key"] = JsonPath.StringAt("$$.Execution.Input.detail.object.key"),
                ["inference"] = JsonPath.StringAt("$.inference"),
                //  ["embeddings"] = JsonPath.StringAt("$.embeddings"),
            },
            //ResultPath = "$"
        });

        var invokeAddToOpenSearch = new LambdaInvoke(this, "AddToOpenSearch", new LambdaInvokeProps
        {
            LambdaFunction = addToOpenSearchFunction,
           // InputPath = "$",
            //ResultSelector = new Dictionary<string, object>
            //{
            //    ["result.$"] = "$.Payload",
            //    ["key.$"] = "$.key",
            //    ["inference.$"] = "$.inference"
            //}
        });

        var success = new Succeed(this, "Success");

        // Define the state machine
        var definition = filterS3Json
            .Next(invokeImageResizer)
            .Next(invokeGetImageInference)
            .Next(invokeGetImageEmbeddings)
            .Next(transformResults)
            .Next(putItem)
            .Next(invokeAddToOpenSearch)
            .Next(success);

        var stateMachineName = $"{props?.AppProps.NamePrefix}-image-ingestion-workflow-{props?.AppProps.NameSuffix}";
        var stateMachine = new StateMachine(this, stateMachineName, new StateMachineProps
        {
            StateMachineName = stateMachineName,
            Definition = definition
        });

        // Create EventBridge rule
        var ruleName = $"{props?.AppProps.NamePrefix}-s3-object-created-rule-{props?.AppProps.NameSuffix}";
        var rule = new Rule(this, ruleName, new RuleProps
        {
            RuleName = ruleName,
            EventPattern = new EventPattern
            {
                Source = new[] { "aws.s3" },
                DetailType = new[] { "Object Created" },
                Detail = new Dictionary<string, object>
                {
                    ["bucket"] = new Dictionary<string, object>
                    {
                        ["name"] = new[] { sourceBucket.BucketName }
                    }
                }
            }
        });

        // Add the Step Function as a target for the EventBridge rule
        rule.AddTarget(new SfnStateMachine(stateMachine));

        // Grant the EventBridge rule permission to start the Step Function execution
        stateMachine.GrantStartExecution(new ServicePrincipal("events.amazonaws.com"));
    }
}