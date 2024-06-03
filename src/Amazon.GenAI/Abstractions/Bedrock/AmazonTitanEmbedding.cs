using System.Text.Json.Nodes;

namespace Amazon.GenAI.Abstractions.Bedrock;

public static class AmazonTitanEmbedding
{
    public static JsonObject CreateBodyJson(string prompt)
    {
        var bodyJson = new JsonObject
        {
            ["inputText"] = prompt,
        };
        return bodyJson;
    }
}