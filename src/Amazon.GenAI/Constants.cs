﻿namespace Amazon.GenAI;

public static class Constants
{
    public static string? Region { get; set; } = "us-west-2";
    public static string KnowledgeBaseName { get; set; } = "octank-hotels";
    public static string TextModelId { get; set; } = "anthropic.claude-3-haiku-20240307-v1:0";
    public static string ImageModelId { get; set; } = "stability.sd3-large-v1:0";
    public static string BucketName { get; set; } = "dotnet-rag-datasource";
    public static string MetadataExtension { get; set; } = ".json";
}