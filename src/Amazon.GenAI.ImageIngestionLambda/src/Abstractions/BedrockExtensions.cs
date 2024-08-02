using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Util;

namespace Amazon.GenAI.ImageIngestionLambda.Abstractions;

internal static class BedrockExtensions
{
    internal static async Task<JsonNode?> InvokeModelAsync(
        this AmazonBedrockRuntimeClient client,
        string? id,
        MemoryStream memoryStream,
        CancellationToken cancellationToken = default)
    {
        memoryStream = memoryStream ?? throw new ArgumentNullException(nameof(memoryStream));

        var response = await client.InvokeModelAsync(new InvokeModelRequest
        {
            ModelId = id,
            Body = memoryStream,
            ContentType = "application/json",
            Accept = "application/json"
        }, cancellationToken).ConfigureAwait(false);

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"InvokeModelAsync failed with status code: {response.HttpStatusCode}");
        }

        return await JsonNode.ParseAsync(
            utf8Json: response.Body,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task<JsonNode?> InvokeModelAsync(
        this AmazonBedrockRuntimeClient client,
        string? id,
        byte[] bytes,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(bytes);

        return await client.InvokeModelAsync(
            id: id,
            memoryStream: stream,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<JsonNode?> InvokeModelAsync(
        this AmazonBedrockRuntimeClient client,
        string? id,
        JsonObject jsonObject,
        CancellationToken cancellationToken = default)
    {
        using var stream = AWSSDKUtils.GenerateMemoryStreamFromString(jsonObject.ToJsonString());

        return await client.InvokeModelAsync(
            id: id,
            memoryStream: stream,
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<T?> InvokeModelAsync<T>(
        this AmazonBedrockRuntimeClient client,
        string id,
        JsonObject jsonObject,
        CancellationToken cancellationToken = default)
    {
        using var stream = AWSSDKUtils.GenerateMemoryStreamFromString(jsonObject.ToJsonString());

        var request = new InvokeModelRequest()
        {
            ContentType = "application/json",
            Accept = "application/json",
            ModelId = id,
            Body = stream
        };

        var response = await client.InvokeModelAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
        {
            throw new InvalidOperationException(
                $"InvokeModelAsync failed with status code: {response.HttpStatusCode}");
        }

        return await JsonSerializer.DeserializeAsync<T>(response.Body, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="stringArray"></param>
    /// <returns></returns>
    public static JsonArray AsArray(this IReadOnlyList<string> stringArray)
    {
        stringArray = stringArray ?? throw new ArgumentNullException(nameof(stringArray));

        var jsonArray = new JsonArray();
        foreach (var arr in stringArray)
        {
            jsonArray.Add(arr);
        }
        return jsonArray;
    }
}