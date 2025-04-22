using OpenSearch.Client;
using OpenSearch.Net.Auth.AwsSigV4;
using System.Text.RegularExpressions;

namespace Amazon.GenAI.Abstractions.OpenSearch;

public class OpenSearchServerlessVectorStore
{
    private readonly OpenSearchVectorStoreOptions _options;
    private readonly OpenSearchClient _client;
    private readonly string? _indexName;

    public OpenSearchServerlessVectorStore(
        OpenSearchVectorStoreOptions options)
    {
        _options = options;
        _indexName = options.IndexName;

        var match = Regex.Match(options.CollectionArn!, @"(?<=\/)[^\/]+$");
        var endpoint = new Uri($"https://{match.Value}.{options.Region?.SystemName}.aoss.amazonaws.com");
        var connection = new AwsSigV4HttpConnection(options.Region, service: AwsSigV4HttpConnection.OpenSearchServerlessService);
        var config = new ConnectionSettings(endpoint, connection);
        _client = new OpenSearchClient(config);

        var existsResponse = _client.Indices.Exists(_indexName);
        if (existsResponse.Exists == false)
        {
            CreateIndex();
        }
    }

    internal void CreateIndex()
    {
        var createIndexResponse = _client?.Indices.Create(_indexName, c => c
            .Settings(x => x
                .Setting("index.knn", true)
            )
            .Map<VectorRecord>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.Path))
                    .Text(t => t.Name(n => n.Text))
                    .Text(t => t.Name(n => n.Base64))
                    .KnnVector(d => d.Name(n => n.Vector).Dimension(_options.Dimensions).Similarity("cosine"))
                )
            ));

        if (createIndexResponse!.IsValid == false)
        {
            throw new Exception(createIndexResponse.DebugInformation);
        }
    }

    internal async Task<IReadOnlyCollection<VectorSearchResponse>> SimilaritySearchByVectorAsync(
        float[] embedding,
        int k = 5,
        CancellationToken cancellationToken = default)
    {
        var searchResponse = await _client!.SearchAsync<VectorRecord>(s => s
            .Index(_indexName)
            .Query(q => q
                .Knn(knn => knn
                    .Field(f => f.Vector)
                    .Vector(embedding)
                    .K(k)
                )
            )).ConfigureAwait(false);

        if (searchResponse!.IsValid == false)
        {
            throw new Exception(searchResponse.DebugInformation);
        }

        return searchResponse.Hits.Select(hit => new VectorSearchResponse
        {
            Score = hit.Score,
            Base64 = hit.Source.Base64,
            Vector = hit.Source.Vector,
            Path = hit.Source.Path,
        }).Take(k).ToList();
    }

    internal async Task<(IReadOnlyCollection<VectorSearchResponse>, long TotalHits)> GetAllAsync(
        int pageSize = 15,
        int pageNumber = 1,
        bool ascending = true)
    {
        var searchRequest = new SearchRequest
        {
            From = (pageNumber - 1) * pageSize,
            Size = pageSize,
            Query = new MatchAllQuery()
        };

        var searchResponse = await _client.SearchAsync<VectorRecord>(searchRequest).ConfigureAwait(false);

        if (searchResponse.IsValid == false)
        {
            throw new Exception($"Error searching documents: {searchResponse.DebugInformation}");
        }

        return (searchResponse.Hits.Select(hit => new VectorSearchResponse
        {
            Score = hit.Score,
            Base64 = hit.Source.Base64,
            Vector = hit.Source.Vector,
            Path = hit.Source.Path,
            Text = hit.Source.Text
        }).ToList(), searchResponse.Total);
    }
}