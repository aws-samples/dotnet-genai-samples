using Amazon.CDK;
using Amazon.CDK.AWS.S3;
using Amazon.CDK.AWS.SSM;

namespace Amazon.GenAI.Cdk;

public static class S3Bucket
{
    public static Bucket Create(KbCustomResourceStack kbCustomResourceStack, KbCustomResourceStackProps props)
    {
        //=========================================================================================
        // A non-publicly accessible Amazon S3 bucket is used to store the knowledge base
        // documents.
        //
        // NOTE: As this is a sample application the bucket is configured to be deleted when
        // the stack is deleted to avoid charges on an unused resource - EVEN IF IT CONTAINS DATA
        // - BEWARE!
        //
        var bucketName = $"{props.AppProps.NamePrefix}-bucket-{props.AppProps.NameSuffix}";
        var bucket = new Bucket(kbCustomResourceStack, bucketName, new BucketProps
        {
            // !DO NOT USE THESE TWO SETTINGS FOR PRODUCTION DEPLOYMENTS - YOU WILL LOSE DATA
            // WHEN THE STACK IS DELETED!
            BucketName = bucketName,
            Versioned = true,
            AutoDeleteObjects = true,
            PublicReadAccess = false,
            RemovalPolicy = RemovalPolicy.DESTROY,
            Encryption = BucketEncryption.S3_MANAGED,
            EventBridgeEnabled = true,
        });

        _ = new StringParameter(kbCustomResourceStack, "KnowledgeBase-BucketName", new StringParameterProps
        {
            ParameterName = $"/{Constants.AppName}/AWS/BucketName",
            StringValue = bucket.BucketName
        });

        return bucket;
    }
}
