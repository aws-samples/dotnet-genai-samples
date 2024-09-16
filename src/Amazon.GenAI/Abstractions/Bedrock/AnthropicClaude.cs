using System.Text.Json.Nodes;
namespace Amazon.GenAI.Abstractions.Bedrock;
public static class AnthropicClaude3
{
    /// <summary>
    /// Creates the request body JSON for the Anthropic model based on the provided prompt and settings.
    /// </summary>
    /// <param name="prompt">The input prompt for the model.</param>
    /// <param name="image">Binary image to use for the request.</param>
    /// <returns>A `JsonObject` representing the request body.</returns>
    public static JsonObject CreateBodyJson(
        string? prompt,
        BinaryData? image = null)
    {
        var bodyJson = new JsonObject
        {
            ["anthropic_version"] = "bedrock-2023-05-31",
            ["max_tokens"] = 2048,
            ["top_p"] = 1,
            ["temperature"] = 0,
            ["messages"] = new JsonArray
            {
                new JsonObject
                {
                    ["role"] = "user",
                    ["content"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "text",
                            ["text"] = prompt,
                        }
                    }
                }
            }
        };
        if (image != null)
        {
            var binaryData = BinaryData.FromBytes(image);
            var base64 = Convert.ToBase64String(binaryData.ToArray());
            var jsonImage = new JsonObject
            {
                ["type"] = "image",
                ["source"] = new JsonObject
                {
                    ["type"] = "base64",
                    ["media_type"] = image.MediaType,
                    ["data"] = base64
                }
            };
            var content = (JsonArray)bodyJson["messages"]?[0]?["content"]!;
            content.Add(jsonImage);
        }
        return bodyJson;
    }
}