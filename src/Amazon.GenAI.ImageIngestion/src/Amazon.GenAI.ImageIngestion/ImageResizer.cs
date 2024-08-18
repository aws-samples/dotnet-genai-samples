using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Amazon.GenAI.ImageIngestion;

public class ImageResizer
{
    private readonly IAmazonS3 _s3Client;
    private readonly string? _destinationBucket;
    private const int TargetWidth = 400;

    public ImageResizer()
    {
        _s3Client = new AmazonS3Client();
        _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    }

    public async Task<object> FunctionHandler(Dictionary<string, string> input, ILambdaContext context)
    {
        Console.WriteLine("in ImageResizer");

        context.Logger.LogInformation($"in ImageResizer.  destination: {_destinationBucket}");

        if (!input.TryGetValue("key", out var key))
        {
            throw new ArgumentException("Image key not provided in the input.");
        }

        if (!input.TryGetValue("bucketName", out var bucketName))
        {
            throw new ArgumentException("bucketName not provided in the input.");
        }

        context.Logger.LogInformation($"key: {key}");
        context.Logger.LogInformation($"bucketName: {bucketName}");

        if (bucketName == null && key == null) return null;

        try
        {
            var response = await _s3Client.GetObjectAsync(bucketName, key);

            using (var imageStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(imageStream);
                imageStream.Position = 0;

				using var image = await Image.LoadAsync(imageStream);
				// Resize the image
				image.Mutate(x => x.Resize(TargetWidth, 0)); // 0 height to maintain aspect ratio

				// Save the resized image to a new stream
				using var outputStream = new MemoryStream();
				await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat!);
				outputStream.Position = 0;

				// Upload the resized image to the destination bucket
				var putRequest = new PutObjectRequest
				{
					BucketName = _destinationBucket,
					Key = key,
					InputStream = outputStream,
					ContentType = response.Headers.ContentType
				};

				var putObjectResponse = await _s3Client.PutObjectAsync(putRequest);
            }

            context.Logger.LogInformation($"Successfully resized {key} and uploaded to {_destinationBucket}");

            return new
            {
                key = key,
                bucketName = _destinationBucket
            };
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error resizing {key}: {e.Message}");
            context.Logger.LogError($"Error resizing {key}: {e.StackTrace}");
            throw;
        }
    }
}