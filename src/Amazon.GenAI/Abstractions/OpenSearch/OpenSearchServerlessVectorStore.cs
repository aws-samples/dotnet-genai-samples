using Amazon.BedrockRuntime;
using OpenSearch.Client;
using Amazon.GenAI.Abstractions.Bedrock;
using Amazon.GenAI.Abstractions.Splitter;
using OpenSearch.Net.Auth.AwsSigV4;
using System.Text.RegularExpressions;

namespace Amazon.GenAI.Abstractions.OpenSearch;

public class OpenSearchServerlessVectorStore
{
    private readonly AmazonBedrockRuntimeClient _bedrockRuntimeClient;
    private readonly string? _embeddingModelId;
    private readonly string? _textModelId;
    private readonly OpenSearchVectorStoreOptions _options;
    private readonly OpenSearchClient _client;
    private readonly string? _indexName;

    public OpenSearchServerlessVectorStore(
        AmazonBedrockRuntimeClient bedrockRuntimeClient,
        string? embeddingModelId,
        string? textModelId,
        OpenSearchVectorStoreOptions options)
    {
        _bedrockRuntimeClient = bedrockRuntimeClient;
        _embeddingModelId = embeddingModelId;
        _textModelId = textModelId;
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
    }

    public async Task<IEnumerable<string>> AddTextDocumentsAsync(
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

    public async Task<bool> AddImageDocumentsAsync(
        Dictionary<string, string> files,
        List<byte[]> cachedImages,
        int chuckSize = 10_000,
        CancellationToken cancellationToken = default)
    {
        var documents = new List<Document>();
        var textModel = new TextModel(_bedrockRuntimeClient, _textModelId);

        var images = new List<Tuple<string, string, BinaryData>>();
        var tasks = new Task<string>[files.Count];

        await GenerateTextFromImages(files, tasks, textModel, images, cachedImages);

        CreateImageDocuments(tasks, images, documents);

        var bulkDescriptor = await CreateBulkDescriptorFromImageDocuments(documents);

        var bulkResponse = await _client!.BulkAsync(bulkDescriptor, cancellationToken)
            .ConfigureAwait(false);

        return bulkResponse.IsValid;
    }

    private async Task GenerateTextFromImages(
        Dictionary<string, string> files,
        Task<string>[] tasks, 
        TextModel textModel, 
        List<Tuple<string, string, BinaryData>> images,
        List<byte[]> cachedImages)
    {
        const string prompt = "Provide a comprehensive description of this image, ensuring you cover every detail with meticulous attention to even the smallest elements.  Describe colors objects and size.  Search the internet for more background information.";
        var i = 0;

        foreach (var path in files.Select(file => file.Value))
        {
            var contentType = EnumerableExtensions.GetMimeType(Path.GetExtension(path)) ?? "";
            var image = BinaryData.FromBytes(cachedImages[i], contentType);
            tasks[i++] = textModel.GenerateAsync(prompt, image);

            images.Add(new Tuple<string, string, BinaryData>(path, contentType, image));
        }

        await Task.WhenAll(tasks);
    }

    private static void CreateImageDocuments(Task<string>[] tasks, List<Tuple<string, string, BinaryData>> images, List<Document> documents)
    {
        for (var j = 0; j < tasks.Length; j++)
        {
            var task = tasks[j];
            var document = new Document
            {
                PageContent = task.Result,
                Metadata = new Dictionary<string, object>()
                {
                    { "path", images[j].Item1 },
                    { "base64", Convert.ToBase64String( images[j].Item3.ToArray()) },
                }
            };

            documents.Add(document);
        }
    }

    private async Task<BulkDescriptor> CreateBulkDescriptorFromImageDocuments(List<Document> documents)
    {
        const int chuckSize = 10_000;
        var bulkDescriptor = new BulkDescriptor();
        foreach (var document in documents)
        {
            var content = document.PageContent.Trim();
            var textSplitter = new RecursiveCharacterTextSplitter(chunkSize: chuckSize);
            var splitText = textSplitter.SplitText(content);
            var embeddings = new List<float[]>(capacity: splitText.Count);
            var embeddingModel = new EmbeddingModel(new AmazonBedrockRuntimeClient(), _embeddingModelId);
            var bytes = Convert.FromBase64String((document.Metadata["base64"] as string)!);
            var image = BinaryData.FromBytes(bytes);
            var embeddingTasks = splitText.Select(text => embeddingModel.CreateEmbeddingsAsync(document.PageContent, image))
                .ToList();
            var results = await Task.WhenAll(embeddingTasks).ConfigureAwait(false);

            foreach (var response in results)
            {
                var embedding = response?["embedding"]?.AsArray();
                if (embedding == null) continue;

                var f = new float[_options.Dimensions!.Value];
                for (var j = 0; j < embedding.Count; j++)
                {
                    f[j] = (float)embedding[j]?.AsValue()!;
                }

                embeddings.Add(f);
            }

            var vectorRecord = new VectorRecord
            {
                Text = document.PageContent,
                Path = document.Metadata["path"] as string,
                Base64 = document.Metadata["base64"] as string,
                Vector = embeddings.ToArray().SelectMany(x => x).ToArray()
            };

            bulkDescriptor.Index<VectorRecord>(desc => desc
                .Document(vectorRecord)
                .Index(_indexName)
            );
        }

        return bulkDescriptor;
    }

    internal async Task<IReadOnlyCollection<VectorSearchResponse>> SimilaritySearchByVectorAsync(
        float[] embedding,
        int k = 4,
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

        return searchResponse.Hits.Select(hit => new VectorSearchResponse
            {
                Score = hit.Score,
                Base64 = hit.Source.Base64,
                Vector = hit.Source.Vector,
                Path = hit.Source.Path,
                Text = hit.Source.Text
            })
            .ToList();
    }
}