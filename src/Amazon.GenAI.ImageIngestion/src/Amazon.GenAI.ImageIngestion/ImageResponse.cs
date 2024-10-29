using System.Text.Json.Serialization;

namespace Amazon.GenAI.ImageIngestion;

public class ImageResponse
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("classifications")]
    public List<string>? Classifications { get; set; }

    [JsonPropertyName("details")]
    public string? Details { get; set; }
}