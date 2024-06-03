namespace Amazon.GenAI.KbLambda;

public class CustomResourceEvent
{
    public CustomResourceEvent()
    {
        Properties = new ResourceProperties();
    }

    public string RequestType { get; set; } = "";
    public ResourceProperties Properties { get; set; }
    public string StackId { get; set; } = "";
    public string RequestId { get; set; } = "";
    public string LogicalResourceId { get; set; } = "";
}