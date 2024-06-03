using System.Text.Json.Nodes;
using Amazon.BedrockRuntime;

namespace Amazon.GenAI.Abstractions.Bedrock;

public class EmbeddingModel(
    AmazonBedrockRuntimeClient bedrockRuntimeClient,
    string? embeddingModelId)
{
    internal async Task<JsonNode?> CreateEmbeddingsAsync(string prompt)
    {
        var bodyJson = AmazonTitanEmbedding.CreateBodyJson(prompt);
        var response = await bedrockRuntimeClient.InvokeModelAsync(embeddingModelId!, bodyJson).ConfigureAwait(false);

        return response;
    }
}