using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;

namespace Amazon.GenAI.KbLambda;

public class SmsParameters : LambdaBaseFunction
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

    public static async Task<string> StoreParameter(string name, string value)
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
}