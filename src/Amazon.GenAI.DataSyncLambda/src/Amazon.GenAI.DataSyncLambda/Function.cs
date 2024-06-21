using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Amazon.GenAI.DataSyncLambda
{
    public class Function
    {
        /// <summary>
        /// A simple function that takes a string and does a ToUpper
        /// </summary>
        /// <param name="evnt">The event for the Lambda function handler to process.</param>
        /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
        /// <returns></returns>
        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            context.Logger.LogLine("in DataSyncLambda lambda");

            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return;
            }

            try
            {
                var knowledgeBaseId = Environment.GetEnvironmentVariable("knowledgeBaseId") ?? "";
                var dataSourceId = Environment.GetEnvironmentVariable("dataSourceId") ?? "";

                var client = new AmazonBedrockAgentClient();

                var request = new StartIngestionJobRequest
                {
                    ClientToken = Guid.NewGuid().ToString(),
                    KnowledgeBaseId = knowledgeBaseId,
                    DataSourceId = dataSourceId
                };
                await client.StartIngestionJobAsync(request);

                context.Logger.LogLine("ran DataSyncLambda");
            }
            catch (Exception e)
            {
                context.Logger.LogLine($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace); throw;
            }

            return ;
        }
    }
}
