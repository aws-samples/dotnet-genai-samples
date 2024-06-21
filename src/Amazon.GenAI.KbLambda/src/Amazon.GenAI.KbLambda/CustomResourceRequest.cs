namespace Amazon.GenAI.KbLambda;

public class CustomResourceRequest
{
    public string? RequestType { get; set; }
    public string? StackId { get; set; }
    public string? RequestId { get; set; }
    public string? LogicalResourceId { get; set; }
    public Dictionary<string, object>? ResourceProperties { get; set; }
}