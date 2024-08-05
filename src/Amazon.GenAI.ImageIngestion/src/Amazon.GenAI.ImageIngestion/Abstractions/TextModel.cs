using Amazon.BedrockRuntime;

namespace Amazon.GenAI.ImageIngestion.Abstractions;

public class TextModel(
    AmazonBedrockRuntimeClient bedrockRuntimeClient,
    string? textModelId)
{
    internal async Task<string> GenerateAsync(string prompt, BinaryData? image = null)
    {
        var bodyJson = AnthropicClaude3.CreateBodyJson(prompt, image);
        var response = await bedrockRuntimeClient.InvokeModelAsync(textModelId!, bodyJson).ConfigureAwait(false);
        var generatedText = response?["content"]?[0]?["text"]?.GetValue<string>() ?? "";

        return generatedText;
    }
}

// Titan Text
// var bodyJson = AmazonTitanText.CreateBodyJson(promptText);
// var response = await BedrockRuntimeClient.InvokeModelAsync(_textModelId!, bodyJson).ConfigureAwait(false);
// var generatedText = response?["results"]?[0]?["outputText"]?.GetValue<string>() ?? string.Empty;