using Amazon.CDK;
using Amazon.CDK.AWS.IAM;

namespace Amazon.GenAI.Cdk;

public class KbCustomResourceStackProps : StackProps
{
    public new Environment Env { get; set; }
    public AppStackProps AppProps { get; set; } = new();
    public Role KbRole { get; set; }
    public Role KbCustomResourceRole { get; set; }
    public Role DataSyncLambdaRole { get; set; }
    public string KnowledgeBaseEmbeddingModelArn { get; set; }
    public string IdentityArn { get; set; }
}