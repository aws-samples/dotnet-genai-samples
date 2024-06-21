using System.Text.Json.Serialization;

namespace TestProject1.Abstractions.Bedrock;

public class AmazonTitanTextToImageResponse
{
    [JsonPropertyName("images")]
    public IReadOnlyList<string> Images { get; set; } = new List<string>();
}
