namespace Shop.Api;

public class ShopOptions
{
    public const string Section = "Shop";

    /// <summary>"InMemory" runs with no AWS dependencies; "Aws" uses DynamoDB and SQS.</summary>
    public string Storage { get; set; } = "InMemory";

    /// <summary>Insert the sample catalog when the products store is empty.</summary>
    public bool SeedProducts { get; set; } = true;

    public AwsResourceOptions Aws { get; set; } = new();

    public ChaosOptions Chaos { get; set; } = new();

    public IntegrationOptions Integrations { get; set; } = new();

    public bool UseAws => string.Equals(Storage, "Aws", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Optional AWS integrations, independent of <see cref="ShopOptions.Storage"/>. Each one is off
/// when its value is empty, so the app still runs with no AWS account.
/// </summary>
public class IntegrationOptions
{
    /// <summary>S3 bucket for order receipts (one JSON object per order). Empty = don't write receipts.</summary>
    public string ReceiptsBucket { get; set; } = "";

    /// <summary>Secrets Manager secret ID or ARN holding the payment gateway API key. Empty = no lookup.</summary>
    public string PaymentSecretId { get; set; } = "";

    public bool UseS3 => !string.IsNullOrWhiteSpace(ReceiptsBucket);
    public bool UseSecretsManager => !string.IsNullOrWhiteSpace(PaymentSecretId);
}

public class AwsResourceOptions
{
    public string ProductsTable { get; set; } = "shop-products";
    public string CartsTable { get; set; } = "shop-carts";
    public string OrdersTable { get; set; } = "shop-orders";
    public string OrdersBySessionIndex { get; set; } = "SessionId-index";
    public string OrderQueueUrl { get; set; } = "";
}

/// <summary>Injects latency and failures into the payment step so traces have something to show.</summary>
public class ChaosOptions
{
    public double PaymentFailureRate { get; set; } = 0.1;
    public int PaymentMinLatencyMs { get; set; } = 50;
    public int PaymentMaxLatencyMs { get; set; } = 600;
}
