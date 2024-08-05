using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Amazon.GenAI.ImageIngestion;

public class AddToOpenSearch
{
    private readonly IAmazonS3 _s3Client;
    private readonly string? _bucketName;
    private const int TargetWidth = 400;

    public AddToOpenSearch()
    {
        _s3Client = new AmazonS3Client();
        _bucketName = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    }

    public async Task<object> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        Console.WriteLine("in AddToOpenSearch");

        context.Logger.LogInformation($"in AddToOpenSearch.  destination: {_bucketName}");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }
        context.Logger.LogInformation($"key: {key}");

        if (input.ContainsKey("inference"))
        {
            var inference = input["inference"];
            context.Logger.LogInformation($"inference: {inference}");
        }

        //if (bucketName == null && key == null) return null;



        //    context.Logger.LogInformation($"Successfully resized {key} and uploaded to {_bucketName}");

        //    return new
        //    {
        //        key = key,
        //        bucketName = _bucketName
        //    };

        return new { key = key };
    }
}