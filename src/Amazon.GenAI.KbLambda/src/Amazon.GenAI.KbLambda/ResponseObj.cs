namespace Amazon.GenAI.KbLambda;

public class ResponseObj
{
    public ResponseObj()
    {
        Data = new ResponseData();
    }

    public ResponseData Data { get; set; }
    public string Status { get; set; } = "";
    public string Reason { get; set; } = "";
    public string StackId { get; set; } = "";
    public string RequestId { get; set; } = "";
    public string LogicalResourceId { get; set; } = "";
}