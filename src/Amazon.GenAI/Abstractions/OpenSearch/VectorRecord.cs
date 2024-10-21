namespace Amazon.GenAI.Abstractions.OpenSearch;

public class VectorRecord
{
    public string? Text { get; set; }
    public required float[] Vector { get; set; }
    public required string? Path { get; set; }
    public string? Base64 { get; set; }
    public string? Classifications { get; set; }
}

public class VectorSearchResponse : VectorRecord
{
    public double? Score { get; set; }
}