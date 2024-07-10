using System.Text.Json.Nodes;

namespace Amazon.GenAI.Abstractions.Bedrock;

public static class AmazonTitanEmbedding
{
    public static JsonObject CreateBodyJson(string prompt, BinaryData? image = null)
    {
        string? base64 = null;

        if (image != null)
        {
            base64 = Convert.ToBase64String(image.ToArray());
        }

        var bodyJson = new JsonObject
        {
            ["inputText"] = prompt,
            ["inputImage"] = base64
        };

        return bodyJson;
    }
}