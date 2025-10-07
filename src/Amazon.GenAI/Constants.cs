using Amazon.BedrockAgent;

namespace Amazon.GenAI;

public static class Constants
{
    public static string? Region { get; set; } = "us-west-2";
    public static string KnowledgeBaseName { get; set; } = "octank-hotels-faq-kb";
    public static string TextModelId { get; set; } = "us.anthropic.claude-3-5-sonnet-20241022-v2:0";
    public static string ClaudeSonnet37InferenceProfileId { get; set; } = "us.anthropic.claude-3-7-sonnet-20250219-v1:0";
    public static string ImageModelId { get; set; } = "stability.sd3-large-v1:0";
    public static string BucketName { get; set; } = "dotnet-rag-datasource";
    public static string MetadataExtension { get; set; } = ".json";
    public static string AgentId { get; set; } = string.Empty;
    public static string AgentAliasId { get; set; } = string.Empty;
}