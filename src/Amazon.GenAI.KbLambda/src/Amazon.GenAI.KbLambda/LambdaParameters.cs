using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Amazon.GenAI.KbLambda;

public class LambdaParameters : LambdaBaseFunction
{
    public static async Task<string?> GetParameter(string name)
    {
        Context?.Logger.LogLine("Getting SSM Parameter");

        try
        {
            var client = new AmazonSimpleSystemsManagementClient();
            var request = new GetParameterRequest { Name = name };
            var response = await client.GetParameterAsync(request);
            return response.Parameter.Value;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static async Task<DeleteParameterResponse> DeleteParameter(string name)
    {
        Context?.Logger.LogLine("Deleting SSM Parameter");

        try
        {
            var client = new AmazonSimpleSystemsManagementClient();
            var request = new DeleteParameterRequest { Name = name };
            var response = await client.DeleteParameterAsync(request);
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}