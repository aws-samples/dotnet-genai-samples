using System.Text.Json.Serialization;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class S3Object
{
	[JsonPropertyName("detail")]
	public S3Detail Detail { get; set; } = new();
}