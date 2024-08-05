using System.Text.Json.Nodes;
using Amazon.BedrockRuntime;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class EmbeddingModel(
    AmazonBedrockRuntimeClient bedrockRuntimeClient,
    string? embeddingModelId)
{
    internal async Task<JsonNode?> CreateEmbeddingsAsync(string prompt, BinaryData? image = null)
    {
        try
        {
            var bodyJson = AmazonTitanEmbedding.CreateBodyJson(prompt, image);
            var response = await bedrockRuntimeClient.InvokeModelAsync(embeddingModelId!, bodyJson).ConfigureAwait(false);

            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}