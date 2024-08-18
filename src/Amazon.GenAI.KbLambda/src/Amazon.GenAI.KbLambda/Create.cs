using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.OpenSearchServerless;
using Amazon.OpenSearchServerless.Model;
using System.Text.Json.Nodes;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.GenAI.KbLambda.Abstractions.OpenSearch;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using OpenSearch.Client;
using OpenSearch.Net.Auth.AwsSigV4;

namespace Amazon.GenAI.KbLambda;

public class Create : LambdaBaseFunction
{
    public static async Task<CollectionDetail> Collection(string? namePrefix, string? nameSuffix)
    {
        Context?.Logger.LogLine("Creating Collection");

        try
        {
            var client = new AmazonOpenSearchServerlessClient();

            var request = new CreateCollectionRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = "VECTORSEARCH",

            };

            var createCollectionResponse = await client.CreateCollectionAsync(request);
            var collectionId = createCollectionResponse.CreateCollectionDetail?.Id!;

            var maxAttempts = 30;
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                Context?.Logger.LogLine($"Checking Collection Status Attempt: {attempts}");

                var batchCollectionRequest = new BatchGetCollectionRequest
                {
                    Ids = [collectionId],
                };

                var batchGetCollectionResponse = await client.BatchGetCollectionAsync(batchCollectionRequest);

                var collections = batchGetCollectionResponse.CollectionDetails;

                if (collections is not null && collections.Count > 0)
                {
                    var collection = collections[0];
                    Context?.Logger.LogLine($"Collection Status: {collection.Status}");

                    if (collection.Status == "ACTIVE")
                    {
                        Context?.Logger.LogLine($"  -- in ACTIVE Collection Status");

                        await StoreParameters(
                                name: $"{namePrefix}-{nameSuffix}/collectionId",
                                value: collection.Id!
                            );

                        await StoreParameters(
                            name: $"{namePrefix}-{nameSuffix}/collectionArn",
                            value: collection.Arn!
                        );

                        await StoreParameters(
                            name: $"{namePrefix}-{nameSuffix}/collectionName",
                            value: collection.Name!
                        );

                        await StoreParameters(
                            name: $"{namePrefix}-{nameSuffix}/collectionEndpoint",
                            value: collection.CollectionEndpoint!
                        );

                        return collection;
                    }
                }

                Thread.Sleep(30000);
                attempts++;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return new CollectionDetail();
    }

    public static Task Index(string host, string? namePrefix, string? nameSuffix)
    {
        Context?.Logger.LogLine("Waiting 90s before creating Index");
        Thread.Sleep(30000);
        Context?.Logger.LogLine("60 more");
        Thread.Sleep(30000);
        Context?.Logger.LogLine("30 more");
        Thread.Sleep(15000);
        Context?.Logger.LogLine("15 more");
        Thread.Sleep(15000);

        Context?.Logger.LogLine("Creating Index");

        try
        {
            var endpoint = new Uri(host);

            Context?.Logger.LogLine($"   -- host: {host}");

            var connection = new AwsSigV4HttpConnection(RegionEndpoint.USEast1, service: AwsSigV4HttpConnection.OpenSearchServerlessService);
            var config = new ConnectionSettings(endpoint, connection);
            var client = new OpenSearchClient(config);

            var createIndexResponse = (client?.Indices.Create($"{namePrefix}-{nameSuffix}", c => c
                .Settings(x => x
                    .Setting("index.knn", true)
                )
                .Map<VectorRecord>(m => m
                    .Properties(p => p
                        .Text(t => t.Name(n => n.Text))
                        .KnnVector(x => x
                            .Name($"{namePrefix}-vector")
                            .Method(n => n.Name("hnsw")
                                .Parameters(p => p.Parameter("ef_construction", 512))
                                .Parameters(p => p.Parameter("m", 12))
                                .Engine("faiss"))
                            .Dimension(1536)
                        )
                    )
                ))!);

            //var createIndexResponse = client?.Indices.Create($"{namePrefix}-{nameSuffix}", c => c
            //    .Settings(x => x
            //        .Setting("index.knn", true)
            //        .Setting("index.knn.space_type", "cosinesimil")
            //    )
            //    .Map<VectorRecord>(m => m
            //        .Properties(p => p
            //            .Keyword(k => k.Name(n => n.Id))
            //            .Text(t => t.Name(n => n.Text))
            //            .KnnVector(d => d.Name(n => n.Vector).Dimension(1536).Similarity("cosine"))
            //        )
            //    ));

            Console.WriteLine($"createIndexResponse.Acknowledged: {createIndexResponse.Acknowledged}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return Task.CompletedTask;
    }

    public static async Task<CreateKnowledgeBaseResponse?> KnowledgeBase(
        string? knowledgeBaseRoleArn,
        string? namePrefix,
        string? nameSuffix,
        string? knowledgeBaseEmbeddingModelArn,
        string? collectionArn)
    {
        Context?.Logger.LogLine("Waiting 60s before creating KnowledgeBase");
        Thread.Sleep(30000);
        Context?.Logger.LogLine("30 more");
        Thread.Sleep(15000);
        Context?.Logger.LogLine("15 more");
        Thread.Sleep(15000);

        Context?.Logger.LogLine("Creating KnowledgeBase");

        try
        {
            var request = new CreateKnowledgeBaseRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                RoleArn = knowledgeBaseRoleArn,
                KnowledgeBaseConfiguration = new KnowledgeBaseConfiguration
                {
                    Type = KnowledgeBaseType.VECTOR,
                    VectorKnowledgeBaseConfiguration = new VectorKnowledgeBaseConfiguration
                    {
                        EmbeddingModelArn = knowledgeBaseEmbeddingModelArn
                    }
                },
                StorageConfiguration = new StorageConfiguration
                {
                    Type = KnowledgeBaseStorageType.OPENSEARCH_SERVERLESS,
                    OpensearchServerlessConfiguration = new OpenSearchServerlessConfiguration
                    {
                        CollectionArn = collectionArn,
                        VectorIndexName = $"{namePrefix}-{nameSuffix}",
                        FieldMapping = new OpenSearchServerlessFieldMapping
                        {

                            VectorField = $"{namePrefix}-vector",
                            TextField = "text",
                            MetadataField = "metadata"
                        }
                    }
                }
            };

            var client = new AmazonBedrockAgentClient();
            var response = await client.CreateKnowledgeBaseAsync(request).ConfigureAwait(false);

            if (response is null ||
                response.KnowledgeBase is null ||
                response.KnowledgeBase.KnowledgeBaseId is null ||
                response.KnowledgeBase.KnowledgeBaseArn is null) return response;

            Context?.Logger.LogLine("   ---- KnowledgeBase created");

            await StoreParameters(
                name: $"{namePrefix}-{nameSuffix}/knowledgeBaseId",
                value: response.KnowledgeBase.KnowledgeBaseId!
            );

            await StoreParameters(
                name: $"{namePrefix}-{nameSuffix}/knowledgeBaseArn",
                value: response.KnowledgeBase.KnowledgeBaseArn!
            );

            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task<CreateDataSourceResponse?> DataSource(
        string? knowledgeBaseBucketArn,
        string knowledgeBaseId,
        string? namePrefix,
        string? nameSuffix
        )
    {
        Context?.Logger.LogLine("Creating DataSource");

        try
        {
            var request = new CreateDataSourceRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                KnowledgeBaseId = knowledgeBaseId,
                DataSourceConfiguration = new DataSourceConfiguration
                {
                    Type = DataSourceType.S3,
                    S3Configuration = new S3DataSourceConfiguration
                    {
                        BucketArn = knowledgeBaseBucketArn,
                    }
                }
            };

            var client = new AmazonBedrockAgentClient();

            var response = await client.CreateDataSourceAsync(request);

            if (response is null ||
                response.DataSource is null ||
                response.DataSource.DataSourceId is null) return response;

            Context?.Logger.LogLine("   ---- DataSource created");

            await StoreParameters(
                name: $"{namePrefix}-{nameSuffix}/dataSourceId",
                value: response.DataSource.DataSourceId!
            );

            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task<string> StoreParameters(string name, string value)
    {
        try
        {
            var client = new AmazonSimpleSystemsManagementClient();

            var request = new PutParameterRequest()
            {
                Name = "/" + name,
                Value = value,
                Type = ParameterType.String,
                Overwrite = true,
                Description = ""
            };
            var response = await client.PutParameterAsync(request);

            return response.Tier;
        }
        catch (Exception e)
        {
            Context?.Logger.LogLine(e.Message);
            throw;
        }
    }

    public static async Task AccessPolicy(string? namePrefix,
        string? nameSuffix,
        string? knowledgeBaseRoleArn,
        string knowledgeBaseCustomResourceRoleArn,
        string? accessPolicyArns)
    {
        Context?.Logger.LogLine("Creating Access Policy");

        var policyJson = new JsonArray
        {
            new JsonObject
            {
                ["Principal"] = new JsonArray
                {
                    { knowledgeBaseRoleArn },
                    { knowledgeBaseCustomResourceRoleArn },
                    { accessPolicyArns }
                },
                ["Description"] = "",
                ["Rules"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["ResourceType"] = "collection",
                        ["Resource"] = new JsonArray
                        {
                            { $"collection/{namePrefix}-{nameSuffix}" }
                        },
                        ["Permission"] = new JsonArray
                        {
                            { "aoss:DescribeCollectionItems" },
                            { "aoss:CreateCollectionItems" },
                            { "aoss:UpdateCollectionItems" }
                        }
                    },
                    new JsonObject
                    {
                        ["ResourceType"] = "index",
                        ["Resource"] = new JsonArray
                        {
                            { $"index/{namePrefix}-{nameSuffix}/*" }
                        },
                        ["Permission"] = new JsonArray
                        {
                            { "aoss:DescribeIndex" },
                            { "aoss:UpdateIndex" },
                            { "aoss:CreateIndex" },
                            { "aoss:DeleteIndex" },
                            { "aoss:ReadDocument" },
                            { "aoss:WriteDocument" }
                        }
                    }
                }
            }
        };

        try
        {
            var request = new CreateAccessPolicyRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = "data",
                Policy = JsonSerializer.Serialize(policyJson)
            };

            var client = new AmazonOpenSearchServerlessClient();
            var response = await client.CreateAccessPolicyAsync(request);

            Context?.Logger.LogLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task NetworkSecurityPolicy(
        string? namePrefix,
        string? nameSuffix
        )
    {
        Context?.Logger.LogLine("Creating Network Security Policy");

        var policyJson = new JsonArray
        {
            new JsonObject
            {
                ["AllowFromPublic"] = true,
                ["Rules"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["ResourceType"] = "dashboard",
                        ["Resource"] = new JsonArray
                        {
                            { $"collection/{namePrefix}-{nameSuffix}" }
                        }
                    },
                    new JsonObject
                    {
                        ["ResourceType"] = "collection",
                        ["Resource"] = new JsonArray
                        {
                            { $"collection/{namePrefix}-{nameSuffix}" }
                        }
                    }
                }
            }
        };

        try
        {
            var request = new CreateSecurityPolicyRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = "network",
                Policy = JsonSerializer.Serialize(policyJson)
            };

            var client = new AmazonOpenSearchServerlessClient();
            var response = await client.CreateSecurityPolicyAsync(request);

            Context?.Logger.LogLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task EncryptionSecurityPolicy(string? namePrefix, string? nameSuffix)
    {
        Context?.Logger.LogLine("Creating Encryption Security Policy");

        var policyJson = new JsonObject
        {
            ["AWSOwnedKey"] = true,
            ["Rules"] = new JsonArray
            {
                new JsonObject
                {
                    ["ResourceType"] = "collection",
                    ["Resource"] = new JsonArray
                    {
                        { $"collection/{namePrefix}-{nameSuffix}" }
                    }
                }
            }
        };

        try
        {
            var request = new CreateSecurityPolicyRequest()
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = "encryption",
                Policy = JsonSerializer.Serialize(policyJson)
            };

            var client = new AmazonOpenSearchServerlessClient();
            var response = await client.CreateSecurityPolicyAsync(request);

            Context?.Logger.LogLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}