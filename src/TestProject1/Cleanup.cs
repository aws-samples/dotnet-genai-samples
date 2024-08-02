using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Lambda;
using Amazon.OpenSearchServerless;
using Amazon.OpenSearchServerless.Model;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.S3;
using Amazon.S3.Model;

namespace TestProject1;

[Parallelizable(ParallelScope.Self)]
public class Cleanup
{
    [Test]
    public async Task DeleteCloudFormationStacks()
    {
        Console.WriteLine("Deleting CloudFormation Stacks");

        try
        {
            var client = new AmazonCloudFormationClient();
            var listResponse = await client.ListStacksAsync();
            var summaries = listResponse.StackSummaries.Where(x => x.StackName.StartsWith("dotnet-genai-kb")).ToList();

            foreach (var item in summaries)
            {
                var request = new DeleteStackRequest { StackName = item.StackName };
                await client.DeleteStackAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteCollections()
    {
        try
        {
            var client = new AmazonOpenSearchServerlessClient();
            var listRequest = new ListCollectionsRequest();
            var listResponse = await client.ListCollectionsAsync(listRequest);
            var summaries = listResponse.CollectionSummaries.Where(x => x.Name.StartsWith("dotnet-genai")).ToList();

            foreach (var item in summaries)
            {
                var request = new DeleteCollectionRequest { Id = item.Id };
                await client.DeleteCollectionAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteAccessPolicies()
    {
        try
        {
            var client = new AmazonOpenSearchServerlessClient();
            var listRequest = new ListAccessPoliciesRequest { Type = AccessPolicyType.Data };
            var listResponse = await client.ListAccessPoliciesAsync(listRequest);
            var summaries = listResponse.AccessPolicySummaries.Where(x => x.Name.StartsWith("dotnet-genai")).ToList();

            foreach (var item in summaries)
            {
                var request = new DeleteAccessPolicyRequest { Name = item.Name, Type = item.Type };
                await client.DeleteAccessPolicyAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteEncryptionPolicies()
    {
        try
        {
            var client = new AmazonOpenSearchServerlessClient();

            var listRequest = new ListSecurityPoliciesRequest { Type = SecurityPolicyType.Encryption };
            var listResponse = await client.ListSecurityPoliciesAsync(listRequest);
            var summaries = listResponse.SecurityPolicySummaries.Where(x => x.Name.StartsWith("dotnet-genai")).ToList();

            foreach (var item in summaries)
            {
                var request = new DeleteSecurityPolicyRequest() { Name = item.Name, Type = item.Type };
                await client.DeleteSecurityPolicyAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteNetworkPolicies()
    {
        try
        {
            var client = new AmazonOpenSearchServerlessClient();

            var listRequest = new ListSecurityPoliciesRequest { Type = SecurityPolicyType.Network };
            var listResponse = await client.ListSecurityPoliciesAsync(listRequest);
            var summaries = listResponse.SecurityPolicySummaries.Where(x => x.Name.StartsWith("dotnet-genai")).ToList();

            foreach (var item in summaries)
            {
                var request = new DeleteSecurityPolicyRequest() { Name = item.Name, Type = item.Type };
                await client.DeleteSecurityPolicyAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteLogs()
    {
        try
        {
            var client = new AmazonCloudWatchClient();

            var listRequest = new ListMetricsRequest();
            var listResponse = await client.ListMetricsAsync(new ListMetricsRequest());
            var summaries = listResponse.Metrics.ToList();

            //foreach (var item in summaries)
            //{
            //    var request = new DeleteSecurityPolicyRequest() { Name = item.Name, Type = item.Type };
            //    await client.DeleteSecurityPolicyAsync(request);
            //}
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteKnowledgeBases()
    {
        try
        {
            var client = new AmazonBedrockAgentClient();
            var listRequest = new ListKnowledgeBasesRequest();
            var listResponse = await client.ListKnowledgeBasesAsync(listRequest);
            var summaries = listResponse.KnowledgeBaseSummaries.Where(x => x.Name.StartsWith("dotnet-genai")).ToList();

            foreach (var request in summaries.Select(item => new DeleteKnowledgeBaseRequest { KnowledgeBaseId = item.KnowledgeBaseId }))
            {
                await client.DeleteKnowledgeBaseAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteLambdas()
    {
        try
        {
            var client = new AmazonLambdaClient();
            var listRequest = new ListFunctionsRequest();
            var listResponse = await client.ListFunctionsAsync(listRequest);
            var functions = listResponse.Functions.Where(x => x.FunctionName.StartsWith("dotnet-genai")).ToList();

            foreach (var request in functions.Select(item => new DeleteFunctionRequest { FunctionName = item.FunctionName }))
            {
                await client.DeleteFunctionAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteSystemManagerParameters()
    {
        try
        {
            var client = new AmazonSimpleSystemsManagementClient();
            var listRequest = new GetParametersRequest { Names = [@"name:*"] };
            var listResponse = await client.GetParametersAsync(listRequest);
            var summaries = listResponse.Parameters.ToList();

            //foreach (var request in summaries.Select(item => new DeleteKnowledgeBaseRequest { KnowledgeBaseId = item.KnowledgeBaseId }))
            //{
            //    await client.DeleteKnowledgeBaseAsync(request);
            //}
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    [Test]
    public async Task DeleteS3Buckets()
    {
        try
        {
            var client = new AmazonS3Client();
            var listRequest = new ListBucketsRequest();
            var listResponse = await client.ListBucketsAsync(listRequest);
            var functions = listResponse.Buckets.Where(x => x.BucketName.StartsWith("dotnet-genai")).ToList();

            foreach (var request in functions.Select(item => new DeleteBucketRequest() { BucketName = item.BucketName }))
            {
                await client.DeleteBucketAsync(request);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}