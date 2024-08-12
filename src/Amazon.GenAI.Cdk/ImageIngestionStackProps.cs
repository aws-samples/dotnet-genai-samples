using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

namespace Amazon.GenAI.Cdk;

public class ImageIngestionStackProps : StackProps
{
    public new Environment Env { get; set; }
    public AppStackProps AppProps { get; set; } = new();
    public Role KbCustomResourceRole { get; set; }
}