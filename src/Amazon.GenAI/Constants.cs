namespace Amazon.GenAI;

public static class Constants
{
    public static string? Region { get; set; } = "us-west-2";
    public static string KnowledgeBaseName { get; set; } = "octank-hotels-faq-kb";
    public static string TextModelId { get; set; } = "anthropic.claude-3-sonnet-20240229-v1:0";
    public static string ImageModelId { get; set; } = "stability.sd3-large-v1:0";
    public static string BucketName { get; set; } = "dotnet-rag-datasource";
    public static string MetadataExtension { get; set; } = ".json";
    public static string[] BuiltinPrompts { get; set; } =
    [
        "What are the check-in and check-out times?",
        "Is there shuttle service to the airport?",
        "Does the room rate include breakfast?",
        "Do you have a fitness center or swimming pool?",
        "Can I bring my pet?",
        "What are the dining options?",
        "What type of views does the room offer?",
        "Is there a concierge service?"
    ];
   public static string AdditionalInstructions { get; set; } = ".Where possible, include the Octank location(s) in your response.";
}