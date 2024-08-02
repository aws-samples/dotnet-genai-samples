using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Amazon.GenAI.Cdk;

public class DynamoDBStack : Construct
{
    public Table Table { get; }

    public DynamoDBStack(Construct scope, string id, IStackConfiguration config) : base(scope, id)
    {
        Table = CreateTable(config);
    }

    private Table CreateTable(IStackConfiguration config)
    {
        var tableName = $"{config.NamePrefix}-images";
        return new Table(this, tableName, new TableProps
        {
            TableName = tableName,
            PartitionKey = new Attribute { Name = "Key", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "Bucket", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY // Be cautious with this in production
        });

    }
}

public class DynamoDbStack : Stack
{
    public DynamoDbStack(Construct scope, string id, DynamoDbStackProps props = null) : base(scope, id, props)
    {
        var tableName = $"{props?.AppProps.NamePrefix}-table-{props?.AppProps.NameSuffix}";
        var table = new Table(this, tableName, new TableProps
        {
            TableName = tableName,
            PartitionKey = new Attribute { Name = "Key", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "Text", Type = AttributeType.STRING },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            RemovalPolicy = RemovalPolicy.DESTROY // Be cautious with this in production
        });

        // Add Global Secondary Index for querying by Location
        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "LocationIndex",
            PartitionKey = new Attribute { Name = "Location", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "Score", Type = AttributeType.NUMBER },
        });

        // Add Global Secondary Index for querying by Score
        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "ScoreIndex",
            PartitionKey = new Attribute { Name = "Key", Type = AttributeType.STRING },
            SortKey = new Attribute { Name = "Score", Type = AttributeType.NUMBER },
        });
    }
}