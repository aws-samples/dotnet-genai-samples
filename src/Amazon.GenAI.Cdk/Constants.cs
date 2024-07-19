using System.Linq;
using Amazon.CDK;

namespace Amazon.GenAI.Cdk;

internal static class Constants
{
	internal static readonly string AppName = "dotnet-genai";
	internal static readonly string EmbeddingModelArn = "arn:aws:bedrock:us-west-2::foundation-model/amazon.titan-embed-text-v2:0";
	internal static readonly string ShareHolderLettersFolder = @"./share-holder-letters";

    public static string ToCurrentDateTime()
    {
        return System.DateTime.Now.ToString("yyyyMMddHHmmss");
    }

    public static BundlingOptions Bundler()
    {
        var buildOption = new BundlingOptions()
        {
            Image = CDK.AWS.Lambda.Runtime.DOTNET_8.BundlingImage,
            User = "root",
            OutputType = BundlingOutput.ARCHIVED,
            Command = new string[]{
                "/bin/sh",
                "-c",
                " dotnet tool install -g Amazon.Lambda.Tools"+
                " && dotnet build"+
                " && dotnet lambda package --output-package /asset-output/function.zip"
            }
        };

        return buildOption;
    }
}

public static class ExtensionMethods
{
    public static string ToHypenCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "-" + x.ToString() : x.ToString())).ToLower();
    }
}
