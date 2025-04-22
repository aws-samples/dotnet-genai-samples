using Amazon.GenAI.ImageIngestion.Abstractions.OpenSearch;
using Amazon.Lambda.Core;
using Amazon.OpenSearchServerless;
using Amazon.OpenSearchServerless.Model;
using OpenSearch.Client;
using OpenSearch.Net.Auth.AwsSigV4;
using System.Text.RegularExpressions;

namespace Amazon.GenAI.ImageIngestion;

public class AddToOpenSearch
{
    private readonly string? _namePrefix;
    private readonly string? _nameSuffix;
    private readonly string? _distributionDomainName;

    public AddToOpenSearch()
    {
        _namePrefix = Environment.GetEnvironmentVariable("NAME_PREFIX");
        _nameSuffix = Environment.GetEnvironmentVariable("NAME_SUFFIX");
        _distributionDomainName = Environment.GetEnvironmentVariable("DISTRIBUTION_DOMAIN_NAME");
    }

    public async Task<BulkDescriptor> FunctionHandler(Dictionary<string, string?> input, ILambdaContext context)
    {
        var key = GetInputValues(input, context, out var origBucketName, out var embeddings);

        var indexName = "my-images-index";
        var (endpoint, client) = await CreateClient();

        var existsResponse = await client.Indices.ExistsAsync(Indices.Parse(indexName));
        if (existsResponse.Exists == false) CreateIndex(client, context, indexName, _namePrefix, _nameSuffix);

        try
        {
            var vectorRecord = new VectorRecord
            {
                OrigBucketName = origBucketName,
                Path = $"{_distributionDomainName}/{key}",
                Vector = embeddings
            };

            var bulkDescriptor = new BulkDescriptor();
            bulkDescriptor.Index<VectorRecord>(desc => desc
                .Document(vectorRecord)
                .Index(indexName)
            );

            var bulkResponse = await client!.BulkAsync(bulkDescriptor)
                .ConfigureAwait(false);

            if (bulkResponse.IsValid == false)
            {
                throw new Exception(bulkResponse.DebugInformation);
            }

            return bulkDescriptor;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void CreateIndex(OpenSearchClient client, ILambdaContext context, string indexName, string? namePrefix,
        string? nameSuffix)
    {
        var createIndexResponse = (client?.Indices.Create(indexName, c => c
            .Settings(x => x
                .Setting("index.knn", true)
            )
            .Map<VectorRecord>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.OrigBucketName))
                    .Text(t => t.Name(n => n.Path))
                    .KnnVector(d => d.Name(n => n.Vector).Dimension(1024).Similarity("cosine"))
                )
            ))!);

        if (createIndexResponse.IsValid == false)
        {
            throw new Exception(createIndexResponse.DebugInformation);
        }
    }

    private static string? GetInputValues(
	    Dictionary<string, string?> input, 
	    ILambdaContext context, 
	    out string? origBucketName,
        out float[] embeddings)
    {
        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        if (!input.TryGetValue("origBucketName", out origBucketName))
        {
            throw new ArgumentException("origBucketName not provided in the input.");
        }

        embeddings = new float[] { };
        if (input.TryGetValue("embeddings", out var arrayString))
        {
            var stringValues = arrayString?.Trim('[', ']').Split(',');
            if (stringValues != null) embeddings = stringValues.Select(float.Parse).ToArray();
        }

        return key;
    }

    private static async Task<(Uri endpoint, OpenSearchClient client)> CreateClient()
    {
        CollectionSummary? collection = null;
        try
        {
            collection =
                (await new AmazonOpenSearchServerlessClient().ListCollectionsAsync(new ListCollectionsRequest()))
                .CollectionSummaries
                .FirstOrDefault(x => x.Name.Contains("dotnet-genai"));
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        var regionEndpoint = RegionEndpoint.USWest2;
        var match = Regex.Match(collection?.Arn!, @"(?<=\/)[^\/]+$");
        var endpoint = new Uri($"https://{match.Value}.{regionEndpoint.SystemName}.aoss.amazonaws.com");
        var connection = new AwsSigV4HttpConnection(regionEndpoint, service: AwsSigV4HttpConnection.OpenSearchServerlessService);
        var config = new ConnectionSettings(endpoint, connection);
        var client = new OpenSearchClient(config);

        return (endpoint, client);
    }
}