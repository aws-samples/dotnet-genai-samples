using Amazon.BedrockRuntime;
using OpenSearch.Client;
using System.Globalization;
using Amazon.GenAI.Abstractions.Bedrock;
using Amazon.GenAI.Abstractions.Splitter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Amazon.GenAI.Abstractions.OpenSearch;

public class OpenSearchVectorStore
{
    private readonly AmazonBedrockRuntimeClient _bedrockRuntimeClient;
    private readonly string? _embeddingModelId;
    private readonly OpenSearchVectorStoreOptions _options;
    private readonly OpenSearchClient _client;
    private readonly string? _indexName;

    public OpenSearchVectorStore(
        AmazonBedrockRuntimeClient bedrockRuntimeClient,
        string? embeddingModelId,
        OpenSearchVectorStoreOptions options)
    {
        _bedrockRuntimeClient = bedrockRuntimeClient;
        _embeddingModelId = embeddingModelId;
        _options = options;
        _indexName = options.ImageIndexName;

        var settings = new ConnectionSettings(options.ConnectionUri)
            .DefaultIndex(options.ImageIndexName)
            .BasicAuthentication(options.Username, options.Password);

        _client = new OpenSearchClient(settings);

        var existsResponse = _client.Indices.Exists(_indexName);
        if (existsResponse.Exists == false)
        {
            CreateIndex();
        }
    }

    public async Task<IEnumerable<string>> AddDocumentsAsync(
        IEnumerable<Document> documents,
        int chuckSize = 10_000,
        CancellationToken cancellationToken = default)
    {
        var embeddingModel = new EmbeddingModel(_bedrockRuntimeClient, _embeddingModelId);
        var bulkDescriptor = new BulkDescriptor();
        var i = 1;

        var enumerable = documents as Document[] ?? documents.ToArray();
        foreach (var document in enumerable)
        {
            var content = document.PageContent.Trim();
            if (string.IsNullOrEmpty(content)) continue;

            var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: chuckSize);
            var splitText = textSplitter.SplitText(content);
            var embeddings = new List<float[]>(capacity: splitText.Count);

            var tasks = splitText.Select(async text => await embeddingModel.CreateEmbeddingsAsync(document.PageContent))
                .ToList();
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);

            foreach (var response in results)
            {
                var embedding = response?["embedding"]?.AsArray();
                if (embedding == null) continue;

                var f = new float[(int)_options.Dimensions!];
                for (var j = 0; j < embedding.Count; j++)
                {
                    f[j] = (float)embedding[j]?.AsValue()!;
                }

                embeddings.Add(f);
            }

            var vectorRecord = new VectorRecord
            {
              //  Id = i++.ToString(CultureInfo.InvariantCulture),
                Text = document.PageContent,
                Path = document.Metadata["path"] as string,
                Vector = embeddings.ToArray().SelectMany(x => x).ToArray()
            };

            bulkDescriptor.Index<VectorRecord>(desc => desc
                .Document(vectorRecord)
                .Index(_indexName)
            );
        }

        var bulkResponse = await _client!.BulkAsync(bulkDescriptor, cancellationToken)
            .ConfigureAwait(false);

        return enumerable.Select(x => x.PageContent);
    }

    //public async Task<IEnumerable<string>> AddImagesAsync(
    //    IEnumerable<Document> documents, 
    //    CancellationToken cancellationToken = default)
    //{
    //    var bulkDescriptor = new BulkDescriptor();
    //    var i = 1;

    //    var enumerable = documents as Document[] ?? documents.ToArray();
    //    foreach (var document in enumerable)
    //    {
    //        document.Metadata.TryGetValue(document.PageContent, out object? value);
    //        var image = (BinaryData)value!;
    //        var images = new List<Data> { Data.FromBytes(image.ToArray()) };

    //        var embeddingRequest = new EmbeddingRequest
    //        {
    //            Strings = new List<string>() { document.PageContent },
    //            Images = images
    //        };
    //        var embed = await EmbeddingModel.CreateEmbeddingsAsync(embeddingRequest, cancellationToken: cancellationToken)
    //            .ConfigureAwait(false);

    //        var vectorRecord = new VectorRecord
    //        {
    //            Id = i++.ToString(CultureInfo.InvariantCulture),
    //            Text = document.PageContent,
    //            Vector = embed.Values.SelectMany(x => x).ToArray()
    //        };

    //        bulkDescriptor.Index<VectorRecord>(desc => desc
    //            .Document(vectorRecord)
    //            .Index(_indexName)
    //        );
    //    }

    //    var bulkResponse = await _client!.BulkAsync(bulkDescriptor, cancellationToken)
    //        .ConfigureAwait(false);

    //    return new List<string>();
    //}

    internal void CreateIndex()
    {
        var createIndexResponse = _client?.Indices.Create(_indexName, c => c
            .Settings(x => x
                .Setting("index.knn", true)
                .Setting("index.knn.space_type", "cosinesimil")
            )
            .Map<VectorRecord>(m => m
                .Properties(p => p
                  //  .Keyword(k => k.Name(n => n.Id))
                    .Text(t => t.Name(n => n.Text))
                    .KnnVector(d => d.Name(n => n.Vector).Dimension(_options.Dimensions).Similarity("cosine"))
                )
            ));
    }

    internal async Task<IEnumerable<Document>> SimilaritySearchByVectorAsync(
        IEnumerable<float> embedding,
        int k = 4,
        CancellationToken cancellationToken = default)
    {
        var searchResponse = await _client!.SearchAsync<VectorRecord>(s => s
            .Index(_indexName)
            .Query(q => q
                .Knn(knn => knn
                    .Field(f => f.Vector)
                    .Vector(embedding.ToArray())
                    .K(k)
                )
            ), cancellationToken).ConfigureAwait(false);

        var documents = searchResponse.Documents
            .Where(vectorRecord => !string.IsNullOrWhiteSpace(vectorRecord.Text))
            .Select(vectorRecord => new Document
            {
                PageContent = vectorRecord.Text!,
            })
            .ToArray();

        return documents;
    }
}