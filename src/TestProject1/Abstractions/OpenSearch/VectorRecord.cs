namespace TestProject1.Abstractions.OpenSearch;

public class VectorRecord
{
    public required string Id { get; set; }
    public string? Text { get; set; }
    public required float[] Vector { get; set; }
    public IDictionary<string, object>? Metadata { get; set; }

}