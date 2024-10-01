using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.CloudFront;
using Amazon.CDK.AWS.CloudFront.Origins;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;
using Amazon.CDK.AWS.Logs;
using Distribution = Amazon.CDK.AWS.CloudFront.Distribution;
using Function = Amazon.CDK.AWS.Lambda.Function;
using FunctionProps = Amazon.CDK.AWS.Lambda.FunctionProps;
using LogGroupProps = Amazon.CDK.AWS.Logs.LogGroupProps;
using Parallel = Amazon.CDK.AWS.StepFunctions.Parallel;

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

        // Create an Origin Access Identity for CloudFront
        var oaiName = $"{props?.AppProps.NamePrefix}-oai-{props.AppProps.NameSuffix}";
        var oai = new OriginAccessIdentity(this, oaiName, new OriginAccessIdentityProps
        {
            Comment = $"OAI for {destinationBucketName}"
        });

        // Create a CloudFront distribution
        var distributionName = $"{props?.AppProps.NamePrefix}-distribution-{props.AppProps.NameSuffix}";
        var distribution = new Distribution(this, distributionName, new DistributionProps
        {
            DefaultBehavior = new BehaviorOptions
            {
                Origin = new S3Origin(destinationBucket, new S3OriginProps
                {
                    OriginAccessIdentity = oai
                }),
                ViewerProtocolPolicy = ViewerProtocolPolicy.REDIRECT_TO_HTTPS,
                AllowedMethods = AllowedMethods.ALLOW_GET_HEAD,
                CachedMethods = CachedMethods.CACHE_GET_HEAD
            },
            PriceClass = PriceClass.PRICE_CLASS_100, // Use only North America and Europe
            HttpVersion = HttpVersion.HTTP2,
            DefaultRootObject = "", // Set this if you have a default page
            ErrorResponses = new[]
            {
                new ErrorResponse
                {
                    HttpStatus = 403,
                    ResponseHttpStatus = 404,
                    ResponsePagePath = "/404.html", // Create this file in your bucket if you want a custom 404 page
                    Ttl = Duration.Seconds(300)
                }
            }
        });

        destinationBucket.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Actions = new[] { "s3:GetBucket*", "s3:GetObject*", "s3:List*" },
            Resources = new[] { destinationBucket.BucketArn, destinationBucket.ArnForObjects("*") },
            Principals = new[] { new CanonicalUserPrincipal(oai.CloudFrontOriginAccessIdentityS3CanonicalUserId) }
        }));

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

        // Define DynamoDB table
        var tableName = $"{props?.AppProps.NamePrefix}-dynamo-table-{props?.AppProps.NameSuffix}";
        var table = new Table(this, tableName, new TableProps
        {
            TableName = tableName,
            PartitionKey = new Attribute { Name = "key", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "bucketName", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST
        });

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
                { "DESTINATION_BUCKET", destinationBucket.BucketName },
                { "NAME_PREFIX", props?.AppProps.NamePrefix },
                { "NAME_SUFFIX", props?.AppProps.NameSuffix },
                { "DISTRIBUTION_DOMAIN_NAME", distribution.DistributionDomainName },
            },
            Role = props.KbCustomResourceRole
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
                ["imageText.$"] = "$.Payload.imageText",
                ["textEmbeddings.$"] = "$.Payload.textEmbeddings",
                ["imageEmbeddings.$"] = "$.Payload.imageEmbeddings"
			}
        });

        var putItemTask = new DynamoPutItem(this, "PutItem", new DynamoPutItemProps
        {
            Table = table,
            Item = new Dictionary<string, DynamoAttributeValue>
            {
                ["key"] = DynamoAttributeValue.FromString(JsonPath.StringAt("$.key")),
                ["bucketName"] = DynamoAttributeValue.FromString(destinationBucketName),
                ["imageText"] = DynamoAttributeValue.FromString(JsonPath.StringAt("$.imageText")),
            }
        });

        var passTask = new Pass(this, "Pass", new PassProps
        {
            Parameters = new Dictionary<string, object>
            {
                ["key.$"] = "$.key",
                ["imageText.$"] = "$.imageText",
                ["textEmbeddings.$"] = "States.JsonToString($.textEmbeddings)",
                ["imageEmbeddings.$"] = "States.JsonToString($.imageEmbeddings)",
            }
        });

        var parallel = new Parallel(this, "Parallel");
        parallel.Branch(putItemTask);
        parallel.Branch(passTask);

        var invokeAddToOpenSearch = new LambdaInvoke(this, "AddToOpenSearch", new LambdaInvokeProps
        {
            LambdaFunction = addToOpenSearchFunction,
            InputPath = "$[1]"
        });

        var success = new Succeed(this, "Success");

        // Define the state machine
        var definition = filterS3Json
            .Next(invokeImageResizer)
            .Next(invokeGetImageInference)
            .Next(invokeGetImageEmbeddings)
            .Next(transformResults)
            .Next(parallel)
            .Next(invokeAddToOpenSearch)
            .Next(success);

        var logGroupName = $"{props?.AppProps.NamePrefix}-add-to-opensearch-log-group-{props?.AppProps.NameSuffix}";
        var logGroup = new LogGroup(this, logGroupName, new LogGroupProps
        {
            LogGroupName = $"/aws/lambda/{props?.AppProps.NamePrefix}-add-to-opensearch-function-{props?.AppProps.NameSuffix}",
            Retention = RetentionDays.ONE_DAY,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var stateMachineName = $"{props?.AppProps.NamePrefix}-image-ingestion-workflow-{props?.AppProps.NameSuffix}";
        var stateMachine = new StateMachine(this, stateMachineName, new StateMachineProps
        {
            StateMachineType = StateMachineType.EXPRESS,
            StateMachineName = stateMachineName,
            Definition = definition,
            Logs = new LogOptions { IncludeExecutionData = true, Level = LogLevel.ALL, Destination = logGroup }
        });

        // New Lambda function for listing and filtering S3 objects
        var listAndFilterS3ObjectsFunctionName = $"{props?.AppProps.NamePrefix}-list-and-filter-s3-objects-{props?.AppProps.NameSuffix}";
        var listAndFilterS3ObjectsFunction = new Function(this, listAndFilterS3ObjectsFunctionName, new FunctionProps
        {
            FunctionName = listAndFilterS3ObjectsFunctionName,
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            Handler = "Amazon.GenAI.ImageIngestion::Amazon.GenAI.ImageIngestion.ListAndFilterS3Objects::FunctionHandler",
            Code = Code.FromAsset("./src/Amazon.GenAI.ImageIngestion/src/Amazon.GenAI.ImageIngestion/bin/Debug/net8.0"),
            Timeout = Duration.Minutes(15),
            MemorySize = 1024,
            Environment = new Dictionary<string, string>
            {
				{ "DESTINATION_BUCKET", destinationBucket.BucketName },
				{ "STATE_MACHINE_ARN", stateMachine.StateMachineArn },
                { "DYNAMODB_TABLE_NAME", table.TableName }
            },
            Role = new Role(this, $"{listAndFilterS3ObjectsFunctionName}-role", new RoleProps
            {
                AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
                ManagedPolicies = new IManagedPolicy[]
                {
                    ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
                    ManagedPolicy.FromAwsManagedPolicyName("AmazonS3ReadOnlyAccess")
                },
                InlinePolicies = new Dictionary<string, PolicyDocument>
                {
                    ["StartStepFunctionExecution"] = new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new[]
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Actions = new[] { "states:StartExecution" },
                                Resources = new[] { stateMachine.StateMachineArn }
                            })
                        }
                    }),
                    ["s3-dynamo-policy"] = new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new[]
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new [] { "*" },
                                Actions = new []
                                {
                                    "dynamodb:GetItem",
                                    "dynamodb:Scan",
                                    "dynamodb:Query",
                                    "dynamodb:BatchGetItem",
                                    "dynamodb:DescribeTable",
                                }
                            }),
                        }
                    })
                }
            })
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

        // Create EventBridge rule for processing existing S3 bucket
        var processExistingBucketRuleName = $"{props?.AppProps.NamePrefix}-process-existing-bucket-rule-{props?.AppProps.NameSuffix}";
        var processExistingBucketRule = new Rule(this, processExistingBucketRuleName, new RuleProps
        {
            RuleName = processExistingBucketRuleName,
            EventPattern = new EventPattern
            {
                Source = new[] { "com.dotnet-genai.imageingestion" },
                DetailType = new[] { "ProcessExistingBucket" }
            }
        });

        // Add the new Lambda function as a target for the process existing bucket rule
        processExistingBucketRule.AddTarget(new LambdaFunction(listAndFilterS3ObjectsFunction));

        // Add the Step Function as a target for the EventBridge rule
        rule.AddTarget(new SfnStateMachine(stateMachine));

        // Grant the EventBridge rule permission to start the Step Function execution
        stateMachine.GrantStartExecution(new ServicePrincipal("events.amazonaws.com"));
        listAndFilterS3ObjectsFunction.GrantInvoke(new ServicePrincipal("events.amazonaws.com"));
    }
}