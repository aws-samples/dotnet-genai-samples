using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using Shop.Contracts;

namespace Shop.Api.Data;

// DynamoDB implementations using the low-level client, so every call shows up
// as its own AWS SDK span (GetItem, Query, PutItem, ...) in traces.

public class DynamoDbProductRepository(IAmazonDynamoDB dynamo, IOptions<ShopOptions> options) : IProductRepository
{
    private readonly string _table = options.Value.Aws.ProductsTable;

    public async Task<IReadOnlyList<ProductDto>> ListAsync(string? category, CancellationToken ct)
    {
        // A scan is fine for a small sample catalog. Use a GSI on Category for real workloads.
        var request = new ScanRequest { TableName = _table };
        if (category is not null)
        {
            request.FilterExpression = "Category = :c";
            request.ExpressionAttributeValues = new() { [":c"] = new AttributeValue { S = category } };
        }

        var response = await dynamo.ScanAsync(request, ct);
        return (response.Items ?? []).Select(ToProduct).OrderBy(p => p.Name).ToList();
    }

    public async Task<ProductDto?> GetAsync(string id, CancellationToken ct)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _table,
            Key = new() { ["Id"] = new AttributeValue { S = id } },
        }, ct);
        return response.Item is { Count: > 0 } item ? ToProduct(item) : null;
    }

    public async Task<bool> AnyAsync(CancellationToken ct)
    {
        var response = await dynamo.ScanAsync(new ScanRequest { TableName = _table, Limit = 1 }, ct);
        return response.Count > 0;
    }

    public Task UpsertAsync(ProductDto p, CancellationToken ct) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = _table,
            Item = new()
            {
                ["Id"] = new AttributeValue { S = p.Id },
                ["Name"] = new AttributeValue { S = p.Name },
                ["Description"] = new AttributeValue { S = p.Description },
                ["Category"] = new AttributeValue { S = p.Category },
                ["Price"] = Number(p.Price),
                ["ImageUrl"] = new AttributeValue { S = p.ImageUrl },
                ["Stock"] = Number(p.Stock),
            },
        }, ct);

    private static ProductDto ToProduct(Dictionary<string, AttributeValue> item) => new(
        item["Id"].S,
        item["Name"].S,
        item["Description"].S,
        item["Category"].S,
        decimal.Parse(item["Price"].N, CultureInfo.InvariantCulture),
        item["ImageUrl"].S,
        int.Parse(item["Stock"].N, CultureInfo.InvariantCulture));

    internal static AttributeValue Number(decimal value) =>
        new() { N = value.ToString(CultureInfo.InvariantCulture) };
}

public class DynamoDbCartRepository(IAmazonDynamoDB dynamo, IOptions<ShopOptions> options) : ICartRepository
{
    private readonly string _table = options.Value.Aws.CartsTable;

    public async Task<CartDto> GetAsync(string sessionId, CancellationToken ct)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _table,
            Key = new() { ["SessionId"] = new AttributeValue { S = sessionId } },
        }, ct);

        if (response.Item is not { Count: > 0 } item) return new CartDto(sessionId, []);
        var items = JsonSerializer.Deserialize<List<CartItemDto>>(item["Items"].S) ?? [];
        return new CartDto(sessionId, items);
    }

    public Task SaveAsync(CartDto cart, CancellationToken ct) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = _table,
            Item = new()
            {
                ["SessionId"] = new AttributeValue { S = cart.SessionId },
                ["Items"] = new AttributeValue { S = JsonSerializer.Serialize(cart.Items) },
                ["UpdatedAt"] = new AttributeValue { S = DateTimeOffset.UtcNow.ToString("O") },
            },
        }, ct);

    public Task DeleteAsync(string sessionId, CancellationToken ct) =>
        dynamo.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _table,
            Key = new() { ["SessionId"] = new AttributeValue { S = sessionId } },
        }, ct);
}

public class DynamoDbOrderRepository(IAmazonDynamoDB dynamo, IOptions<ShopOptions> options) : IOrderRepository
{
    private readonly AwsResourceOptions _aws = options.Value.Aws;

    public Task CreateAsync(OrderDto o, CancellationToken ct) =>
        dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = _aws.OrdersTable,
            Item = new()
            {
                ["OrderId"] = new AttributeValue { S = o.OrderId },
                ["SessionId"] = new AttributeValue { S = o.SessionId },
                ["CustomerName"] = new AttributeValue { S = o.CustomerName },
                ["Email"] = new AttributeValue { S = o.Email },
                ["ShippingAddress"] = new AttributeValue { S = o.ShippingAddress },
                ["Items"] = new AttributeValue { S = JsonSerializer.Serialize(o.Items) },
                ["Total"] = DynamoDbProductRepository.Number(o.Total),
                ["Status"] = new AttributeValue { S = o.Status },
                ["CreatedAt"] = new AttributeValue { S = o.CreatedAt.ToString("O") },
            },
        }, ct);

    public async Task<OrderDto?> GetAsync(string orderId, CancellationToken ct)
    {
        var response = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = _aws.OrdersTable,
            Key = new() { ["OrderId"] = new AttributeValue { S = orderId } },
        }, ct);
        return response.Item is { Count: > 0 } item ? ToOrder(item) : null;
    }

    public async Task<IReadOnlyList<OrderDto>> ListBySessionAsync(string sessionId, CancellationToken ct)
    {
        var response = await dynamo.QueryAsync(new QueryRequest
        {
            TableName = _aws.OrdersTable,
            IndexName = _aws.OrdersBySessionIndex,
            KeyConditionExpression = "SessionId = :s",
            ExpressionAttributeValues = new() { [":s"] = new AttributeValue { S = sessionId } },
        }, ct);
        return (response.Items ?? []).Select(ToOrder).OrderByDescending(o => o.CreatedAt).ToList();
    }

    public Task UpdateStatusAsync(string orderId, string status, CancellationToken ct) =>
        dynamo.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _aws.OrdersTable,
            Key = new() { ["OrderId"] = new AttributeValue { S = orderId } },
            UpdateExpression = "SET #s = :s",
            ExpressionAttributeNames = new() { ["#s"] = "Status" },
            ExpressionAttributeValues = new() { [":s"] = new AttributeValue { S = status } },
        }, ct);

    private static OrderDto ToOrder(Dictionary<string, AttributeValue> item) => new(
        item["OrderId"].S,
        item["SessionId"].S,
        item["CustomerName"].S,
        item["Email"].S,
        item["ShippingAddress"].S,
        JsonSerializer.Deserialize<List<CartItemDto>>(item["Items"].S) ?? [],
        decimal.Parse(item["Total"].N, CultureInfo.InvariantCulture),
        item["Status"].S,
        DateTimeOffset.Parse(item["CreatedAt"].S, CultureInfo.InvariantCulture));
}
