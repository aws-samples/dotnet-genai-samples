namespace Amazon.GenAI.KbLambda;

public class CustomResourceResponse
{
    public string? Status { get; set; }
    public string? PhysicalResourceId { get; set; }
    public string? StackId { get; set; }
    public string? RequestId { get; set; }
    public string? LogicalResourceId { get; set; }
    public string? Reason { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}