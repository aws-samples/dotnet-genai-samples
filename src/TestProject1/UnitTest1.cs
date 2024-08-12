using System.Diagnostics;
using System.Text.Json;
using Amazon.OpenSearchServerless;
using Amazon.IdentityManagement;
using Amazon.OpenSearchServerless.Model;
using OpenSearch.Client;
using System.Text.Json.Nodes;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.IdentityManagement.Model;
using Amazon.Runtime.Internal;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using OpenSearch.Net.Auth.AwsSigV4;
using TestProject1.Abstractions.OpenSearch;
using Environment = Amazon.CDK.Environment;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;

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

        //var accessPolicy = AccessPolicy(_namePrefix, _nameSuffix);
        //accessPolicy.Wait();
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
    public async Task CanCreateIndex()
    {
        var sw = new Stopwatch();
        sw.Start();

        await CreateIndexWrapper(5);

        sw.Stop();

        Console.WriteLine($"duration: {sw.ElapsedMilliseconds}");
        Console.WriteLine($"duration: {sw.Elapsed.Minutes}");
    }

    private async Task CreateIndexWrapper(int attempt)
    {
        if (CollectionDetail?.CollectionEndpoint is null)
        {
            await CreateCollection();
        }

        CollectionDetail = new CollectionDetail
        {
            CollectionEndpoint = CollectionDetail?.CollectionEndpoint
        };

        Console.WriteLine("Creating Index");

        Console.WriteLine($"  --- Attempt: {attempt}");
        Console.WriteLine("Please wait 60 secs");
        Thread.Sleep(30000);
        Console.WriteLine("30 more");
        Thread.Sleep(15000);
        Console.WriteLine("15 more");
        Thread.Sleep(15000);

        var isAcknowledged = await CreateIndex();
        if (isAcknowledged == false && attempt > 0) await CreateIndexWrapper(--attempt);
    }

    private async Task<bool> CreateIndex()
    {
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
                        .KnnVector(x => x
                            .Name(n => $"{_namePrefix}-vector")
                            .Method(n => n.Name("hnsw")
                                .Parameters(p => p.Parameter("ef_construction", 512))
                                .Parameters(p => p.Parameter("m", 12))
                                .Engine("faiss"))
                            .Dimension(1024)
                        )
                    )
                ))!).ConfigureAwait(false);

            Console.WriteLine($"createIndexResponse.Acknowledged: {createIndexResponse.Acknowledged}");
            Console.WriteLine($"createIndexResponse.DebugInformation: {createIndexResponse.DebugInformation}");
            return createIndexResponse.Acknowledged;
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
                        CollectionArn = CollectionDetail?.Arn,
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

    [TestCase("6o64y4qxwb2k8vcyusp8", "dotnet-genai-20240707022407")]
    public async Task DoesIndexExist(string uniqueIdentifier, string indexName)
    {
        var endpoint = new Uri($"https://{uniqueIdentifier}.us-east-1.aoss.amazonaws.com");
        var connection = new AwsSigV4HttpConnection(RegionEndpoint.USEast1, service: AwsSigV4HttpConnection.OpenSearchServerlessService);
        var config = new ConnectionSettings(endpoint, connection);
        var client = new OpenSearchClient(config);

        var response = await client.Indices.ExistsAsync(new IndexExistsRequest(indexName));
    }

    [Test]
    public async Task DocumentModelDynamoDb()
    {
        const string tableName = "dotnet-genai-results-table-test";
        const string key = "clipart2.png";
        const string bucketName = "dotnet-genai-destination-test";
        var client = new AmazonDynamoDBClient();

        Table table = Table.LoadTable(client, tableName);

        Console.WriteLine("\n*** Executing Retrieve ***");
        // Optional configuration.
        GetItemOperationConfig config = new GetItemOperationConfig
        {
            AttributesToGet = new List<string> { "key", "bucketName", "inference" },
            ConsistentRead = true
        };

        var find = new Dictionary<string, DynamoDBEntry>
        {
            { "key", key },
            { "bucketName", bucketName }
        };

        Document document = await table.GetItemAsync(find, config);
        Console.WriteLine("Retrieve: Printing Table retrieved...");

        Console.WriteLine(document["key"].ToString());
        Console.WriteLine(document["bucketName"].ToString());
        Console.WriteLine(document["inference"].ToString());
    }

    [Test]
    public async Task QueryDynamoDb()
    {
        const string tableName = "dotnet-genai-results-table-test";
        const string key = "clipart2.png";
        const string bucketName = "dotnet-genai-destination-test";

        var dynamoDbClient = new AmazonDynamoDBClient();
        var items = new List<Dictionary<string, AttributeValue>>();

        // First, describe the table to get its key schema
        var tableDescription = await dynamoDbClient.DescribeTableAsync(tableName);
        var keySchema = tableDescription.Table.KeySchema;

        if (keySchema.Count == 0)
        {
            throw new Exception("Table has no key schema");
        }

        var queryRequest = new QueryRequest
        {
            TableName = tableName,
            ExpressionAttributeNames = new Dictionary<string, string>(),
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>()
        };

        string keyConditionExpression = "";

        foreach (var keyElement in keySchema)
        {
            string attrName = keyElement.AttributeName;
            string placeholderName = $"#attr_{attrName}";
            string placeholderValue = $":value_{attrName}";

            queryRequest.ExpressionAttributeNames[placeholderName] = attrName;

            if (attrName.ToLower() == "key")
            {
                queryRequest.ExpressionAttributeValues[placeholderValue] = new AttributeValue { S = key };
            }
            else if (attrName.ToLower() == "bucketname")
            {
                queryRequest.ExpressionAttributeValues[placeholderValue] = new AttributeValue { S = bucketName };
            }

            if (!string.IsNullOrEmpty(keyConditionExpression))
            {
                keyConditionExpression += " AND ";
            }
            keyConditionExpression += $"{placeholderName} = {placeholderValue}";
        }

        queryRequest.KeyConditionExpression = keyConditionExpression;

        var response = await dynamoDbClient.QueryAsync(queryRequest);
        items.AddRange(response.Items);

        Console.WriteLine($"count: {response.Count}");
        Console.WriteLine($"response: {JsonSerializer.Serialize(response.Items)}");
    }

    public static async Task AccessPolicy(
        string? namePrefix,
        string? nameSuffix
        // string? knowledgeBaseRoleArn
        )
    {
        Console.WriteLine("Creating Access Policy");

        var policyJson = new JsonArray
        {
            new JsonObject
            {
                ["Principal"] = new JsonArray
                {
                    //{ knowledgeBaseRoleArn },
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
        var role = await client.GetRoleAsync(new GetRoleRequest { RoleName = "dotnet-genai-role-20240602084839" });
        return role.Role;
    }

    public static async Task<Role> GetKnowledgeBaseRole()
    {
        var client = new AmazonIdentityManagementServiceClient();
        var role = await client.GetRoleAsync(new GetRoleRequest { RoleName = "dotnet-genai-role-20240602084839" });
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