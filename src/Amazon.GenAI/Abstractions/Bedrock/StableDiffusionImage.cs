using System.Text.Json.Nodes;

namespace Amazon.GenAI.Abstractions.Bedrock;

public static class StableDiffusionImage
{
    public static JsonObject CreateBodyJson(string prompt)
    {
        var bodyJson = new JsonObject
        {
            ["prompt"] = prompt,
            ["mode"] = "text-to-image",
            ["aspect_ratio"] = "9:16",
            ["output_format"] = "jpeg",
        };
        return bodyJson;
    }
}