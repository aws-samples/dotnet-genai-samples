using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class S3Stack : Construct
{
    public Bucket SourceBucket { get; }
    public Bucket DestinationBucket { get; }

    public S3Stack(Construct scope, string id, IStackConfiguration config) : base(scope, id)
    {
        SourceBucket = CreateBucket(BucketType.Source, config);
        DestinationBucket = CreateBucket(BucketType.Destination, config);
    }

    private Bucket CreateBucket(BucketType bucketType, IStackConfiguration config)
    {
        var bucketName = $"{config.NamePrefix}-{bucketType.ToString().ToLower()}-bucket-{config.NameSuffix}";
        return new Bucket(this, bucketName, new BucketProps
        {
            BucketName = bucketName,
            Versioned = true,
            RemovalPolicy = RemovalPolicy.DESTROY,
            AutoDeleteObjects = true
        });
    }

    private enum BucketType
    {
        Source,
        Destination
    }
}