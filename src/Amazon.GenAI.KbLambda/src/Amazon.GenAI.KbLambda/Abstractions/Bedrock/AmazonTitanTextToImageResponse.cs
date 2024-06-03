using System.Text.Json.Serialization;

namespace Amazon.GenAI.KbLambda.Abstractions.Bedrock;

public class AmazonTitanTextToImageResponse
{
    [JsonPropertyName("images")]
    public IReadOnlyList<string> Images { get; set; } = new List<string>();
}
