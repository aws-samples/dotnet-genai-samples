namespace Amazon.GenAI.Cdk;

using Amazon.CDK;

public class DynamoDbStackProps : StackProps
{
    public new Environment Env { get; set; }
    public AppStackProps AppProps { get; set; } = new();
}