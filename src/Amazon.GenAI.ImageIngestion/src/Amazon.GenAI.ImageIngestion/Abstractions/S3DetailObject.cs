using System.Text.Json.Serialization;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class S3DetailObject
{
	[JsonPropertyName("key")]
	public string Key { get; set; }
}