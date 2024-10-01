namespace Amazon.GenAI;

public static class Constants
{
    public static string? Region { get; set; } = "us-west-2";
    public static string KnowledgeBaseName { get; set; } = "octank-hotels";
    public static string TextModelId { get; set; } = "anthropic.claude-3-sonnet-20240229-v1:0";
    public static string ImageModelId { get; set; } = "stability.sd3-large-v1:0";
    public static string BucketName { get; set; } = "dotnet-rag-datasource";
    public static string MetadataExtension { get; set; } = ".json";
    public static string[] BuiltinPrompts { get; set; } =
    [
        "What are the check-in and check-out times",
        "Is there a free shuttle service from the airport?",
        "Does the room rate include breakfast?",
        "Do you have a fitness center or swimming pool?",
        "Is there an on-site restaurant available?",
        "Is there room service available?",
        "What type of views does the room offer?",
        "How far is the hotel from the Las Vegas Strip?",
        "Are there complimentary drinks?",
        "Are there complimentary betting vouchers for guests?",
        "Are quiet, non-gaming spaces available?",
        "Is there a concierge service?"
    ];
}