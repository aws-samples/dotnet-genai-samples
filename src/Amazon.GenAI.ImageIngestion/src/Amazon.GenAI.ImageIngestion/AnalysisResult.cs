namespace Amazon.GenAI.ImageIngestion;

public class AnalysisResult
{
    public List<string> DetectedText { get; set; } = new();
    public float Confidence { get; set; }
}