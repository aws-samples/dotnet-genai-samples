using System.Text.Json;
using Amazon.OpenSearchServerless;
using Amazon.IdentityManagement;
using Amazon.OpenSearchServerless.Model;
using OpenSearch.Client;
using System.Text.Json.Nodes;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.IdentityManagement.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using OpenSearch.Net.Auth.AwsSigV4;
using TestProject1.Abstractions.OpenSearch;
using Environment = Amazon.CDK.Environment;

namespace TestProject1;

public class Tests
{
    public CollectionDetail? CollectionDetail { get; set; }
    private static string? _namePrefix;
    private static string? _nameSuffix;
    private static Environment? _environment;

    [SetUp]
    public void Setup()
    {
        var identityTask = GetIdentity();
        identityTask.Wait();
        var identity = identityTask.Result;

        //_namePrefix = "test";
        //_nameSuffix = DateTime.Now.ToString("yyyyMMddHHmmss");

        _namePrefix = "dotnet-genai";
        _nameSuffix = "20240602140856";

        _environment = MakeEnv();

        //var networkSecurityPolicy = NetworkSecurityPolicy(_namePrefix, _nameSuffix);
        //networkSecurityPolicy.Wait();

        //var encryptionSecurityPolicy = EncryptionSecurityPolicy(_namePrefix, _nameSuffix);
        //encryptionSecurityPolicy.Wait();
    }

    [TearDown]
    public void TearDown()
    {
        var client = new AmazonOpenSearchServerlessClient();

        //var request = new DeleteCollectionRequest
        //{
        //    ClientToken = Guid.NewGuid().ToString(),
        //    Id = CollectionDetail!.Id
        //};
        //var deleteCollectionTask = client.DeleteCollectionAsync(request);
        //deleteCollectionTask.Wait();
    }

    [Test]
    public async Task CreateCollection()
    {
        Console.WriteLine("Creating Collection");

        try
        {
            var client = new AmazonOpenSearchServerlessClient();

            var request = new CreateCollectionRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{_namePrefix}-{_nameSuffix}",
                Type = "VECTORSEARCH",

            };

            var createCollectionResponse = await client.CreateCollectionAsync(request);
            var collectionId = createCollectionResponse.CreateCollectionDetail?.Id!;

            var maxAttempts = 30;
            var attempts = 0;

            while (attempts < maxAttempts)
            {
                Console.WriteLine($"Checking Collection Status Attempt: {attempts}");

                var batchCollectionRequest = new BatchGetCollectionRequest
                {
                    Ids = [collectionId],
                };

                var batchGetCollectionResponse = await client.BatchGetCollectionAsync(batchCollectionRequest);

                var collections = batchGetCollectionResponse.CollectionDetails;

                if (collections is not null && collections.Count > 0)
                {
                    var collection = collections[0];
                    Console.WriteLine($"Collection Status: {collection.Status}");

                    if (collection.Status == "ACTIVE")
                    {
                        Console.WriteLine($"  -- in ACTIVE Collection Status");
                        Console.WriteLine($"  -- trying to store: {collection.Id}");
                        Console.WriteLine($"  -- trying to store: {collection.Arn}");
                        Console.WriteLine($"  -- trying to store: {_namePrefix}-{_nameSuffix}/collectionId");

                        CollectionDetail = collection;

                        break;
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
    }

    [Test]
    public async Task CreateIndex()
    {
        //   await CreateCollection();
        CollectionDetail = new CollectionDetail
        {
            CollectionEndpoint = "https://fl7ahga4v8bgwpajvpfh.us-east-1.aoss.amazonaws.com"
        };

        Console.WriteLine("Creating Index");

        try
        {
            var endpoint = new Uri(CollectionDetail?.CollectionEndpoint!);

            Console.WriteLine($"   -- host: {CollectionDetail?.CollectionEndpoint!}");

            var connection = new AwsSigV4HttpConnection(service: AwsSigV4HttpConnection.OpenSearchServerlessService);
            var config = new ConnectionSettings(endpoint, connection);
            var client = new OpenSearchClient(config);

            var createIndexResponse = await (client?.Indices.CreateAsync($"{_namePrefix}-{_nameSuffix}", c => c
                .Settings(x => x
                    .Setting("index.knn", true)
                )
                .Map<VectorRecord>(m => m
                    .Properties(p => p
                        //.FieldAlias(a=>a.Name($"{namePrefix}-vector"))
                        //.Keyword(k => k.Name(n => n.Id))
                        //.Nested<Dictionary<string, object>>(n => n.Name(x => x.Metadata))
                        // .Text(t => t.Name(n => n.Text))
                        .KnnVector(x => x
                                .Name(n => $"{_namePrefix}-vector")
                                //.Method(n => n.Name("hnsw")
                                //    .Parameters(p => p.Parameter("ef_construction", 512))
                                //    .Parameters(p => p.Parameter("m", 12))
                                //    .Engine("faiss"))
                                .Dimension(1024)
                        // .Similarity("cosine")
                        )
                    )
                ))!).ConfigureAwait(false);

            Console.WriteLine($"createIndexResponse.Acknowledged: {createIndexResponse.Acknowledged}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task CreateKnowledgeBase()
    {
        await CreateIndex();

        Console.WriteLine("Creating KnowledgeBase");

        try
        {
            var knowledgeBaseRole = await GetKnowledgeBaseRole();

            var request = new CreateKnowledgeBaseRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{_namePrefix}-{_nameSuffix}",
                RoleArn = knowledgeBaseRole.Arn,
                KnowledgeBaseConfiguration = new KnowledgeBaseConfiguration
                {
                    Type = KnowledgeBaseType.VECTOR,
                    VectorKnowledgeBaseConfiguration = new VectorKnowledgeBaseConfiguration
                    {
                        EmbeddingModelArn = "arn:aws:bedrock:us-east-1::foundation-model/amazon.titan-embed-text-v2:0"
                    }
                },
                StorageConfiguration = new StorageConfiguration
                {
                    Type = KnowledgeBaseStorageType.OPENSEARCH_SERVERLESS,
                    OpensearchServerlessConfiguration = new OpenSearchServerlessConfiguration
                    {
                        CollectionArn = CollectionDetail.Arn,
                        VectorIndexName = $"{_namePrefix}-{_nameSuffix}",
                        FieldMapping = new OpenSearchServerlessFieldMapping
                        {
                            VectorField = $"{_namePrefix}-vector",
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
                response.KnowledgeBase.KnowledgeBaseArn is null) return;

            Console.WriteLine("   ---- KnowledgeBase created");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    [Test]
    public async Task DeleteKnowledgeBaseStacks()
    {

    }

    public static async Task AccessPolicy(
    string? namePrefix,
    string? nameSuffix,
    string? knowledgeBaseRoleArn,
    string? knowledgeBaseCustomResourceRoleArn
    )
    {
        Console.WriteLine("Creating Access Policy");

        var policyJson = new JsonArray
        {
            new JsonObject
            {
                ["Principal"] = new JsonArray
                {
                    { knowledgeBaseRoleArn },
                    { "arn:aws:iam::676229420717:user/dev_acct" }
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

            Console.WriteLine($"HttpStatusCode: {response.HttpStatusCode}");
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
        Console.WriteLine("Creating Network Security Policy");

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

            Console.WriteLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task EncryptionSecurityPolicy(string? namePrefix, string? nameSuffix)
    {
        Console.WriteLine("Creating Encryption Security Policy");

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

            Console.WriteLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task<Role> GetKnowledgeBaseCustomResourceRole()
    {
        var client = new AmazonIdentityManagementServiceClient();
        var role = await client.GetRoleAsync(new GetRoleRequest { RoleName = "dotnet-genai-kb-role-20240602084839" });
        return role.Role;
    }

    public static async Task<Role> GetKnowledgeBaseRole()
    {
        var client = new AmazonIdentityManagementServiceClient();
        var role = await client.GetRoleAsync(new GetRoleRequest { RoleName = "dotnet-genai-kb-role-20240602084839" });
        return role.Role;
    }

    private static Environment MakeEnv(string account = null, string region = null)
    {
        return new Environment
        {
            Account = account ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
            Region = region ?? System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION")
        };
    }

    private static async Task<GetCallerIdentityResponse> GetIdentity()
    {
        var client = new AmazonSecurityTokenServiceClient();
        var response = await client.GetCallerIdentityAsync(new GetCallerIdentityRequest());

        return response;
    }
}