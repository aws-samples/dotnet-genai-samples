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

    public bool UseAws => string.Equals(Storage, "Aws", StringComparison.OrdinalIgnoreCase);
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
