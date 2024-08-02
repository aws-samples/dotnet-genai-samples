using System.Text.Json;
using Amazon.OpenSearchServerless.Model;
using Amazon.OpenSearchServerless;
using OpenSearch.Client;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;

namespace Amazon.GenAI.KbLambda;

public class Delete : LambdaBaseFunction
{
    public static async Task AccessPolicy(string nameSuffix, string namePrefix)
    {
        Context?.Logger.LogLine("Deleting Access Policy");

        try
        {
            var request = new DeleteAccessPolicyRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = "data"
            };

            var client = new AmazonOpenSearchServerlessClient();
            var response = await client.DeleteAccessPolicyAsync(request);

            Context?.Logger.LogLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task SecurityPolicy(string nameSuffix, string namePrefix, string type)
    {
        Context?.Logger.LogLine("Deleting Network Security Policy");

        try
        {
            var request = new DeleteSecurityPolicyRequest
            {
                ClientToken = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{nameSuffix}",
                Type = type,
            };

            var client = new AmazonOpenSearchServerlessClient();
            var response = await client.DeleteSecurityPolicyAsync(request);

            Context?.Logger.LogLine($"HttpStatusCode: {response.HttpStatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task Collection(string nameSuffix, string namePrefix)
    {
        Context?.Logger.LogLine("Deleting Collection");

        try
        {
            var client = new AmazonOpenSearchServerlessClient();

            var collectionId = await SmsParameters.GetParameter(name: $"/{namePrefix}-{nameSuffix}/collectionId");
            var request = new DeleteCollectionRequest
            {
                Id = collectionId
            };

            var response = await client.DeleteCollectionAsync(request);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task KnowledgeBase(string nameSuffix, string namePrefix)
    {
        Context?.Logger.LogLine("Deleting Knowledgebase");

        try
        {
            var client = new AmazonBedrockAgentClient();

            var knowledgeBaseId = await SmsParameters.GetParameter(name: $"/{namePrefix}-{nameSuffix}/knowledgeBaseId");
            var request = new DeleteKnowledgeBaseRequest
            {
                KnowledgeBaseId = knowledgeBaseId
            };

            var response = await client.DeleteKnowledgeBaseAsync(request);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}