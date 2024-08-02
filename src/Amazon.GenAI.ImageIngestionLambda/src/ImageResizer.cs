using Amazon.GenAI.ImageIngestionLambda.Abstractions;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Amazon.GenAI.ImageIngestionLambda;

public class ImageResizer
{
    private readonly IAmazonS3 _s3Client;
    private const int TargetWidth = 400; // Adjust as needed

    public ImageResizer()
    {
        _s3Client = new AmazonS3Client();
    }

    public async Task<Dictionary<string, string>> FunctionHandler(S3Record s3, ILambdaContext context)
    {
        context.Logger.LogLine("in ImageResizer lambda");
        s3 = s3 ?? throw new ArgumentNullException(nameof(s3));

        context = context ?? throw new ArgumentNullException(nameof(context));
        var destinationBucketName = Environment.GetEnvironmentVariable("destinationBucketName") ?? "";
        context.Logger.LogLine($"destinationBucketName: {destinationBucketName}");

        var bucketName = s3.Bucket;
        var key = s3.Key;
        context.Logger.LogLine($"bucketName: {bucketName}");
        context.Logger.LogLine($"key: {key}");

        try
        {
            var response = await _s3Client.GetObjectAsync(bucketName, key);

            using (var imageStream = new MemoryStream())
            {
                await response.ResponseStream.CopyToAsync(imageStream);
                imageStream.Position = 0;

                using (var image = await Image.LoadAsync(imageStream))
                {
                    // Resize the image
                    image.Mutate(x => x.Resize(TargetWidth, 0)); // 0 height to maintain aspect ratio

                    // Save the resized image to a new stream
                    using (var outputStream = new MemoryStream())
                    {
                        await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat!);
                        outputStream.Position = 0;

                        // Upload the resized image to the destination bucket
                        var putRequest = new PutObjectRequest
                        {
                            BucketName = destinationBucketName,
                            Key = $"resized-{key}",
                            InputStream = outputStream,
                            ContentType = response.Headers.ContentType
                        };

                        await _s3Client.PutObjectAsync(putRequest);
                    }
                }
            }

            context.Logger.LogInformation($"Successfully resized {key} and uploaded to {destinationBucketName}");

            // Return the key of the resized image
            var resizedKey = $"resized-{key}";
            return new Dictionary<string, string>
            {
                { "key", resizedKey }
            };
        }
        catch (Exception e)
        {
            context.Logger.LogLine($"Error processing {key} from bucket {bucketName}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogLine(e.Message);
            context.Logger.LogLine(e.StackTrace);
            throw;
        }
    }
}