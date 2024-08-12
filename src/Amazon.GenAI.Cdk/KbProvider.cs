using System;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.CustomResources;
using Constructs;
using System.Collections.Generic;
using Amazon.CDK.AWS.S3;

namespace Amazon.GenAI.Cdk;

public class KbProvider
{
    public static Provider Get(KbCustomResourceStack kbCustomResourceStack, KbCustomResourceStackProps props,
        Bucket bucket)
    {
        var kbCustomResourceLambda = CreateKnowledgeBaseCustomResourceLambda(kbCustomResourceStack, props, bucket);

        var providerName = $"{props.AppProps.NamePrefix}-provider-{props.AppProps.NameSuffix}";
        var provider = new Provider(kbCustomResourceStack, providerName, new ProviderProps
        {
            ProviderFunctionName = providerName,
            OnEventHandler = kbCustomResourceLambda,
            LogRetention = RetentionDays.THREE_DAYS,
        });

        return provider;
    }

    private static Function CreateKnowledgeBaseCustomResourceLambda(Construct kbCustomResource,
        KbCustomResourceStackProps props, Bucket bucket)
    {
        var functionName = $"{props.AppProps.NamePrefix}-lambda-{props.AppProps.NameSuffix}";
        var handler = "Amazon.GenAI.KbLambda::Amazon.GenAI.KbLambda.Function::FunctionHandler";
        var lambdaFunction = new Function(kbCustomResource, functionName, new FunctionProps
        {
            Runtime = CDK.AWS.Lambda.Runtime.DOTNET_8,
            MemorySize = 1024,
            FunctionName = functionName,
           // LogRetention = RetentionDays.ONE_DAY,
            Role = props.KbCustomResourceRole,
            Handler = handler,
            Code = Code.FromAsset("./src/Amazon.GenAI.KbLambda/src/Amazon.GenAI.KbLambda", new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = Constants.Bundler()
            }),
            Environment = new Dictionary<string, string>
            {
                ["namePrefix"] = props.AppProps.NamePrefix,
                ["nameSuffix"] = props.AppProps.NameSuffix,
                ["accessPolicyArns"] = props.IdentityArn,
                ["knowledgeBaseRoleArn"] = props.KbRole.RoleArn,
                ["knowledgeBaseCustomResourceRoleArn"] = props.KbCustomResourceRole.RoleArn,
                ["knowledgeBaseEmbeddingModelArn"] = props.KnowledgeBaseEmbeddingModelArn,
                ["knowledgeBaseBucketArn"] = bucket.BucketArn,
            },
            Timeout = Duration.Minutes(15),
        });

        return lambdaFunction;
    }
}
