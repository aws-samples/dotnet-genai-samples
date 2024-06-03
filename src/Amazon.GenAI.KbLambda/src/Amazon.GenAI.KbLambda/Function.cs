using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Amazon.GenAI.KbLambda;

public class Function
{
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="evnt">The event for the Lambda function handler to process.</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns></returns>
    public async Task<CustomResourceResponse> FunctionHandler(CustomResourceRequest evnt, ILambdaContext? context)
    {
        context = context ?? throw new ArgumentNullException(nameof(context));

        context.Logger.LogLine("in KbLambda lambda");

        var namePrefix = Environment.GetEnvironmentVariable("namePrefix") ?? "";
        var nameSuffix = Environment.GetEnvironmentVariable("nameSuffix") ?? "";
        var knowledgeBaseCustomResourceRoleArn = Environment.GetEnvironmentVariable("knowledgeBaseCustomResourceRoleArn") ?? "";
        var knowledgeBaseRoleArn = Environment.GetEnvironmentVariable("knowledgeBaseRoleArn") ?? "";
        var accessPolicyArns = Environment.GetEnvironmentVariable("accessPolicyArns") ?? "";
        var knowledgeBaseEmbeddingModelArn = Environment.GetEnvironmentVariable("knowledgeBaseEmbeddingModelArn") ?? "";
        var knowledgeBaseBucketArn = Environment.GetEnvironmentVariable("knowledgeBaseBucketArn") ?? "";

        var response = new CustomResourceResponse
        {
            Status = "SUCCESS",
            PhysicalResourceId = "CustomResourcePhysicalID",
            StackId = evnt.StackId,
            RequestId = evnt.RequestId,
            LogicalResourceId = evnt.LogicalResourceId,
            Data = new Dictionary<string, object>()
        };

        var serialize = JsonSerializer.Serialize(evnt);
        context.Logger.LogLine("evnt");
        context.Logger.LogLine(serialize);

        Create.SetContext(context);

        try
        {
            switch (evnt.RequestType)
            {
                case "Create":
                    context.Logger.LogLine("Create");

                    await Create.AccessPolicy(
                       namePrefix: namePrefix,
                       nameSuffix: nameSuffix,
                       knowledgeBaseRoleArn: knowledgeBaseRoleArn,
                       knowledgeBaseCustomResourceRoleArn: knowledgeBaseCustomResourceRoleArn,
                       accessPolicyArns: accessPolicyArns
                       );

                    await Create.NetworkSecurityPolicy(
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix
                    );

                    await Create.EncryptionSecurityPolicy(
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix
                    );

                    var collection = await Create.Collection(
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix
                    );

                    await Create.Index(
                        host: collection.CollectionEndpoint!,
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix
                    );

                    var knowledgeBase = await Create.KnowledgeBase(
                        knowledgeBaseRoleArn: knowledgeBaseRoleArn,
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix,
                        knowledgeBaseEmbeddingModelArn: knowledgeBaseEmbeddingModelArn,
                        collectionArn: collection.Arn!
                    );

                    var dataSource = await Create.DataSource(
                        knowledgeBaseBucketArn: knowledgeBaseBucketArn,
                        knowledgeBaseId: knowledgeBase?.KnowledgeBase?.KnowledgeBaseId!,
                        namePrefix: namePrefix,
                        nameSuffix: nameSuffix
                    );

                    await StartDataSync(knowledgeBase, dataSource);

                    //response.Data = new ResponseData()
                    //{
                    //    //CollectionArn = collection.Arn!,
                    //    //CollectionId = collection.Id!,
                    //    //CollectionName = collection.Name!,
                    //    //CollectionEndpoint = collection.CollectionEndpoint,
                    //    //DataSourceId = dataSource.dataSource.dataSourceId,
                    //    //KnowledgeBaseId = knowledgeBase.KnowledgeBase.KnowledgeBaseId,
                    //};
                    response.Reason = "CreateKnowledgeBase successful";
                    break;

                case "Update":
                    context.Logger.LogLine("Update");

                    //var collectionInfo = await Update.Collection(
                    //    nameSuffix: requestProperties.NameSuffix,
                    //    namePrefix: requestProperties.NamePrefix
                    //);

                    //var knowledgeBaseInfo = await Update.KnowledgeBase(
                    //    nameSuffix: requestProperties.NameSuffix,
                    //    namePrefix: requestProperties.NamePrefix
                    //);

                    //response.Data = new ResponseData()
                    //{
                    //    CollectionArn = collectionInfo.CollectionArn,
                    //    CollectionId = collectionInfo.CollectionId,
                    //    CollectionName = collectionInfo.CollectionName,
                    //    CollectionEndpoint = collectionInfo.CollectionEndpoint,
                    //    DataSourceId = knowledgeBaseInfo.DataSourceId,
                    //    KnowledgeBaseId = knowledgeBaseInfo.KnowledgeBaseId,
                    //};
                    response.Reason = "UpdateKnowledgeBase successful";
                    break;


                case "Delete":
                    context.Logger.LogLine("Delete");

                    //await Delete.AccessPolicy(
                    //    nameSuffix: nameSuffix,
                    //    namePrefix: namePrefix
                    //);
                    //await Delete.SecurityPolicy(
                    //    nameSuffix: nameSuffix,
                    //    namePrefix: namePrefix,
                    //    type: "network"
                    //);
                    //await Delete.SecurityPolicy(
                    //    nameSuffix: nameSuffix,
                    //    namePrefix: namePrefix,
                    //    type: "encryption"
                    //);
                    //await Delete.Collection(
                    //    nameSuffix: nameSuffix,
                    //    namePrefix: namePrefix
                    //);
                    //await Delete.KnowledgeBase(
                    //    nameSuffix: nameSuffix,
                    //    namePrefix: namePrefix
                    //);
                    //await deleteParameter(
                    //    name: $"{requestProperties.NamePrefix}-{ requestProperties.NameSuffix}/ collectionArn"
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ collectionEndpoint`,
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ collectionId`,
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ collectionName`,
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ dataSourceId`,
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ knowledgeBaseArn`,
                    //);
                    //await deleteParameter(
                    //    name: `/${ requestProperties.namePrefix}
                    //    -${ requestProperties.nameSuffix}/ knowledgeBaseId`,
                    //);

                    response.Reason = "DeleteKnowledgeBase successful";
                    break;
            }

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.ToString()}");

            throw;
            return new CustomResourceResponse
            {
                Status = "FAILED",
                PhysicalResourceId = "CustomResourcePhysicalID",
                StackId = evnt.StackId,
                RequestId = evnt.RequestId,
                LogicalResourceId = evnt.LogicalResourceId,
                Reason = ex.Message
            };
            throw;
        }
    }

    private static async Task StartDataSync(CreateKnowledgeBaseResponse? knowledgeBase,
        CreateDataSourceResponse? dataSource)
    {
        var client = new AmazonBedrockAgentClient();
        var request = new StartIngestionJobRequest
        {
            ClientToken = Guid.NewGuid().ToString(),
            KnowledgeBaseId = knowledgeBase?.KnowledgeBase?.KnowledgeBaseId,
            DataSourceId = dataSource?.DataSource.DataSourceId
        };
        await client.StartIngestionJobAsync(request);
    }
}