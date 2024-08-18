using System.Text.Json.Serialization;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class S3DetailBucket
{
	[JsonPropertyName("name")]
	public string? Name { get; set; }
}