using System.Text.Json.Nodes;

namespace Amazon.GenAI.ImageIngestionLambda.Abstractions;

public static class AnthropicClaude3
{
    public static JsonObject CreateBodyJson(
    string? prompt,
    BinaryData? image = null)
    {
        var bodyJson = new JsonObject
        {
            ["anthropic_version"] = "bedrock-2023-05-31",
            ["max_tokens"] = 8000,
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
                    ["media_type"] = image.MediaType ?? "image/jpeg",
                    ["data"] = base64
                }
            };

            var content = (JsonArray)bodyJson["messages"]?[0]?["content"]!;
            content.Add(jsonImage);
        }

        return bodyJson;
    }
}