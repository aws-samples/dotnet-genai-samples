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
    private readonly string? _bucketName;
    private readonly string? _namePrefix;
    private readonly string? _nameSuffix;
    private readonly string? _distributionDomainName;

    public AddToOpenSearch()
    {
        _bucketName = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
        _namePrefix = Environment.GetEnvironmentVariable("NAME_PREFIX");
        _nameSuffix = Environment.GetEnvironmentVariable("NAME_SUFFIX");
        _distributionDomainName = Environment.GetEnvironmentVariable("DISTRIBUTION_DOMAIN_NAME");
    }

    public async Task<BulkDescriptor> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        Console.WriteLine("in AddToOpenSearch");

        context.Logger.LogInformation($"in AddToOpenSearch.  destination: {_bucketName}");

        var key = GetInputValues(input, context, 
            out var imageText, 
	        out var textEmbeddings,
	        out var imageEmbeddings,
            out var classifications
        );

        const string indexName = "my-images-index";
        var (endpoint, client) = await CreateClient();
        Console.WriteLine($"endpointUrl: {endpoint.AbsoluteUri}");

		var existsResponse = await client.Indices.ExistsAsync(Indices.Parse(indexName));
        if (existsResponse.Exists == false) CreateIndex(client, context, indexName, _namePrefix, _nameSuffix);

        try
        {
            var vectorRecord = new VectorRecord
            {
                Text = imageText,
                Path = $"{_distributionDomainName}/{key}",
                Classifications = classifications,
                Vector = imageEmbeddings
            };

            var bulkDescriptor = new BulkDescriptor();
            bulkDescriptor.Index<VectorRecord>(desc => desc
                .Document(vectorRecord)
                .Index(indexName)
            );

            var bulkResponse = await client!.BulkAsync(bulkDescriptor)
                .ConfigureAwait(false);

            Console.WriteLine($"bulkResponse for Text is IsValid: {bulkResponse.IsValid}");
            context.Logger.LogInformation($"bulkResponse DebugInformation: {bulkResponse.DebugInformation}");

            if (bulkResponse.IsValid == false)
            {
                throw new Exception(bulkResponse.DebugInformation);
            }

            return new BulkDescriptor();
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
        context.Logger.LogInformation("Creating Index...");

        var createIndexResponse = (client?.Indices.Create(indexName, c => c
            .Settings(x => x
                .Setting("index.knn", true)
            )
            .Map<VectorRecord>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.Text))
                    .Text(t => t.Name(n => n.Classifications))
                    .Text(t => t.Name(n => n.Path))
                    .KnnVector(d => d.Name(n => n.Vector).Dimension(1024).Similarity("cosine"))
                )
            ))!);

        context.Logger.LogInformation($"createIndexResponse Acknowledged: {createIndexResponse.Acknowledged}");
        context.Logger.LogInformation($"createIndexResponse DebugInformation: {createIndexResponse.DebugInformation}");

        if (createIndexResponse.IsValid == false)
        {
            throw new Exception(createIndexResponse.DebugInformation);
        }
    }

    private static string? GetInputValues(
	    Dictionary<string, string> input, 
	    ILambdaContext context, 
	    out string imageText,
        out float[] textEmbeddings,
	    out float[] imageEmbeddings,
        out string classifications
        )
    {
        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {key}");

        imageText = "";
        if (input.TryGetValue("imageText", out var value))
        {
	        imageText = value;
            context.Logger.LogInformation($"imageText: {imageText}");
        }

        textEmbeddings = new float[] { };
        //if (input.TryGetValue("textEmbeddings", out var arrayTextString))
        //{
	       // var stringValues = arrayTextString.Trim('[', ']').Split(',');
	       // textEmbeddings = stringValues.Select(float.Parse).ToArray();
	       // context.Logger.LogInformation($"textEmbeddings: {textEmbeddings}");
        //}

		imageEmbeddings = new float[] { };
        if (input.TryGetValue("imageEmbeddings", out var arrayImageString))
        {
            var stringValues = arrayImageString.Trim('[', ']').Split(',');
            imageEmbeddings = stringValues.Select(float.Parse).ToArray();
            context.Logger.LogInformation($"imageEmbeddings: {stringValues}");
        }

        classifications = "";
        if (input.TryGetValue("classifications", out var value2))
        {
            classifications = value2;
            context.Logger.LogInformation($"classifications: {classifications}");
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

            Console.WriteLine($"collectionArn: {collection?.Arn}");
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