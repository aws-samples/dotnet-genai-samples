using System.Text.Json.Nodes;

namespace Amazon.GenAI.ImageIngestionLambda.Abstractions;

public static class AmazonTitanEmbedding
{
    public static JsonObject CreateBodyJson(string prompt, BinaryData? image = null)
    {
        var bodyJson = new JsonObject
        {
            ["inputText"] = prompt
        };

        if (image != null)
        {
            bodyJson["inputImage"] = Convert.ToBase64String(image.ToArray());
        }

        return bodyJson;
    }
}