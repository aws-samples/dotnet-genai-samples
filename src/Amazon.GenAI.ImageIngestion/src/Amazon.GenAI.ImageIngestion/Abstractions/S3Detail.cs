using System.Text.Json.Serialization;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class S3Detail
{
	[JsonPropertyName("bucket")]
	public S3DetailBucket Bucket { get; set; } = new();
	[JsonPropertyName("object")]
	public S3DetailObject Object { get; set; } = new();
	[JsonPropertyName("bucketName")]
	public string? BucketName { get; set; }
}