using Amazon.CDK;

namespace Amazon.GenAI.Cdk;

internal sealed class Program
{
    public static void Main()
    {
        var app = new App();

        var env = MakeEnv();

        var appStackProp = new AppStackProps();
        appStackProp.NameSuffix = "test";
        var embeddingModelArn = $"arn:aws:bedrock:{env.Region}::foundation-model/amazon.titan-embed-text-v2:0";

        var kbCustomResourceName = $"{appStackProp.NamePrefix}-cr-{appStackProp.NameSuffix}";
        var kbCustomResourceStack = new KbCustomResourceStack(app, kbCustomResourceName, new KbCustomResourceStackProps
        {
            Env = env,
            AppProps = appStackProp,
            KnowledgeBaseEmbeddingModelArn = embeddingModelArn
        });

        //var imageStackName = $"{appStackProp.NamePrefix}-image-ingestion-{appStackProp.NameSuffix}";
        //var imageStack = new ImageIngestionStack(app, imageStackName, new ImageIngestionStackProps
        //{
        //    Env = env,
        //    AppProps = appStackProp,
        //});

        app.Synth();
    }

    private static Environment MakeEnv(string account = null, string region = null)
    {
        return new Environment
        {
            Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
            Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
        };
    }
}
