using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

namespace Amazon.GenAI.Cdk;

public class ImageIngestionStackProps : StackProps, IStackConfiguration
{
    public ImageIngestionStackProps()
    {
        NamePrefix = AppProps.NamePrefix;
        NameSuffix = AppProps.NameSuffix;
    }

    public new Environment Env { get; set; }
    public AppStackProps AppProps { get; set; } = new();
    public string NamePrefix { get; set; }
    public string NameSuffix { get; set; }
}