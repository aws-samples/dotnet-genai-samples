using Amazon.CDK;
using Constructs;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.S3.Notifications;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.S3.Deployment;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;

namespace Amazon.GenAI.Cdk;

public class KbCustomResourceStack : Stack
{
    public CustomResource KbCustomResource { get; init; }
    public Bucket Bucket { get; init; }
    public Role KbCustomResourceRole { get; set; }

    internal KbCustomResourceStack(Construct scope, string id, KbCustomResourceStackProps props = null) : base(scope, id, props)
    {
        var identityTask = GetIdentity();
        identityTask.Wait();
        var identity = identityTask.Result;

        props!.IdentityArn = identity.Arn;
        props.KbCustomResourceRole = CreateKnowledgeBaseCustomResourceRole(this, props);
        KbCustomResourceRole = props.KbCustomResourceRole;
        props.KbRole = CreateKnowledgeBaseServiceRole(this, props);
        props.DataSyncLambdaRole = CreateDataSyncLambdaRole(this, props);

        Bucket = S3Bucket.Create(this, props);

        var provider = KbProvider.Get(this, props, Bucket);

        var kbCustomResourceName = $"{props.AppProps.NamePrefix}-cr-{props.AppProps.NameSuffix}";
        KbCustomResource = new CustomResource(this, kbCustomResourceName, new CustomResourceProps
        {
            ServiceToken = provider.ServiceToken,
        });

        var dataSyncLambda = CreateDataSyncLambda(this, props);
        Bucket.GrantReadWrite(props!.KbCustomResourceRole);
        Bucket.GrantReadWrite(props.KbRole);
        Bucket.GrantReadWrite(dataSyncLambda);
        Bucket.AddEventNotification(EventType.OBJECT_CREATED, new LambdaDestination(dataSyncLambda));
    }

    private Role CreateKnowledgeBaseServiceRole(Construct kbSecurity, KbCustomResourceStackProps props)
    {
        var roleName = $"{props.AppProps.NamePrefix}-service-role-{props.AppProps.NameSuffix}";
        var role = new Role(kbSecurity, roleName, new RoleProps
        {
            RoleName = roleName,
            AssumedBy = new CompositePrincipal(
                new ServicePrincipal("bedrock.amazonaws.com"),
                new ServicePrincipal("lambda.amazonaws.com"),
                new ArnPrincipal(props.IdentityArn)
            ),
            InlinePolicies = new Dictionary<string, PolicyDocument>
            {
                {
                    "foundation-model-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Actions = new []
                                {
                                    "bedrock:InvokeModel",
                                },
                                Resources = new [] { props.KnowledgeBaseEmbeddingModelArn },
                            }),
                        },
                    })
                },
                {
                    "aoss-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Actions = new []
                                {
                                    "aoss:APIAccessAll",
                                    "iam:CreateServiceLinkedRole"
                                },
                                Resources = new [] { $"arn:aws:aoss:{props.Env.Region}:{props.Env.Account}:collection/*" },
                            }),
                        },
                    })
                },
                {
                    "s3-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Actions = new []
                                {
                                    "s3:ListBucket"
                                },
                                Resources = new []
                                {
                                    $"arn:aws:s3:::*"
                                },
                            }),
                        },
                    })
                },
                {
                    "ssm-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new []
                                {
                                    $"arn:aws:ssm:{props.Env.Region}:{props.Env.Account}:*"
                                },
                                Actions = new []
                                {
                                    "ssm:PutParameter",
                                    "ssm:GetParameter",
                                    "ssm:DeleteParameter",
                                }
                            }),
                        },
                    })
                },
            }
        });

        return role;
    }

    private static Role CreateKnowledgeBaseCustomResourceRole(Construct kbSecurity, KbCustomResourceStackProps props)
    {
        var kbCustomResourceRoleName = $"{props.AppProps.NamePrefix}-cr-role-{props.AppProps.NameSuffix}";
        var kbCustomResourceRole = new Role(kbSecurity, kbCustomResourceRoleName, new RoleProps
        {
            RoleName = kbCustomResourceRoleName,
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            InlinePolicies = new Dictionary<string, PolicyDocument>
            {
                {
                    "bedrock-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new [] { "*" },
                                Actions = new []
                                {
                                    "bedrock:*KnowledgeBase",
                                    "bedrock:*DataSource",
                                    "bedrock:StartIngestionJob",
                                    "iam:PassRole"
                                }
                            }),
                        },
                    })
                },
                {
                    "ssm-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new []
                                {
                                    $"arn:aws:ssm:{props.Env.Region}:{props.Env.Account}:*"
                                },
                                Actions = new []
                                {
                                    "ssm:PutParameter",
                                    "ssm:GetParameter",
                                    "ssm:DeleteParameter",
                                }
                            }),
                        },
                    })
                },
                {
                    "aoss-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new [] { "*" },
                                Actions = new []
                                {
                                    "aoss:*",
                                    "iam:CreateServiceLinkedRole"
                                }
                            }),
                        },
                    })
                },
                {
                    "dynamo-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
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
                                    "dynamodb:BatchGetItem"
                                }
                            }),
                        },
                    })
                },
            },
            ManagedPolicies = new[]
            {
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
            }
        });

        return kbCustomResourceRole;
    }

    private static Role CreateDataSyncLambdaRole(Construct kbSecurity, KbCustomResourceStackProps props)
    {
        var lambdaRoleName = $"{props.AppProps.NamePrefix}-datasync-lambda-role-{props.AppProps.NameSuffix}";
        var kbRole = new Role(kbSecurity, lambdaRoleName, new RoleProps
        {
            RoleName = lambdaRoleName,
            AssumedBy = new ServicePrincipal("lambda.amazonaws.com"),
            InlinePolicies = new Dictionary<string, PolicyDocument>
            {
                {
                    "bedrock-policy",
                    new PolicyDocument(new PolicyDocumentProps
                    {
                        Statements = new []
                        {
                            new PolicyStatement(new PolicyStatementProps
                            {
                                Effect = Effect.ALLOW,
                                Resources = new [] { "*" },
                                Actions = new []
                                {
                                    "bedrock:*",
                                }
                            }),
                        },
                    })
                }
            },
            ManagedPolicies = new[]
            {
                ManagedPolicy.FromAwsManagedPolicyName("service-role/AWSLambdaBasicExecutionRole"),
            }
        });

        return kbRole;
    }

    private static Function CreateDataSyncLambda(
        Construct kbCustomResource,
        KbCustomResourceStackProps props)
    {
        var functionName = $"{Constants.AppName.ToHypenCase()}-datasync-lambda-{Constants.ToCurrentDateTime()}";
        var handler = $"Amazon.GenAI.DataSyncLambda::Amazon.GenAI.DataSyncLambda.Function::FunctionHandler";
        var lambdaFunction = new Function(kbCustomResource, functionName, new FunctionProps
        {
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            MemorySize = 1024,
            FunctionName = functionName,
            LogRetention = RetentionDays.ONE_DAY,
            Role = props.DataSyncLambdaRole,
            Handler = handler,
            Code = Code.FromAsset("./src/Amazon.GenAI.DataSyncLambda/src/Amazon.GenAI.DataSyncLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Timeout = Duration.Minutes(5),
        });

        return lambdaFunction;
    }

    private static async Task<GetCallerIdentityResponse> GetIdentity()
    {
        var client = new AmazonSecurityTokenServiceClient();
        var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest());
        return response;
    }
}