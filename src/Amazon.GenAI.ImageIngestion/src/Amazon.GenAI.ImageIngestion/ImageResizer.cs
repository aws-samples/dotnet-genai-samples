using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace Amazon.GenAI.ImageIngestion;

public class ImageResizer
{
    private readonly IAmazonS3 _s3Client;
    private const int TargetWidth = 400; // Adjust as needed
    private readonly string? _destinationBucket;

    public ImageResizer()
    {
        _s3Client = new AmazonS3Client();
        _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
    }

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Event = evnt.Records?[0].S3;
        if (s3Event == null) return;

        try
        {
            var response = await _s3Client.GetObjectAsync(s3Event.Bucket.Name, s3Event.Object.Key);

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
                            BucketName = _destinationBucket,
                            Key = $"resized-{s3Event.Object.Key}",
                            InputStream = outputStream,
                            ContentType = response.Headers.ContentType
                        };

                        await _s3Client.PutObjectAsync(putRequest);
                    }
                }
            }

            context.Logger.LogInformation($"Successfully resized {s3Event.Object.Key} and uploaded to {_destinationBucket}");
        }
        catch (Exception e)
        {
            context.Logger.LogError($"Error resizing {s3Event.Object.Key}: {e.Message}");
            throw;
        }
    }
}