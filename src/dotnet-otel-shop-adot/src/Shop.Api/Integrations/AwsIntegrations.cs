using System.Diagnostics;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Options;
using Shop.Api.Diagnostics;
using Shop.Contracts;

namespace Shop.Api.Integrations;

// Optional AWS integrations used during checkout. There is no tracing code here: the ADOT
// auto-instrumentation wraps every AWS SDK call in a client span (for example
// "SecretsManager.GetSecretValue" and "S3.PutObject"), and Application Signals reports S3 and
// Secrets Manager as dependencies of shop-api. Each integration falls back to a no-op when its
// setting is empty (see IntegrationOptions), so the app still runs without an AWS account.

/// <summary>Supplies the payment gateway API key used to authorize a charge.</summary>
public interface IPaymentCredentialProvider
{
    Task<string> GetApiKeyAsync(CancellationToken ct);
}

/// <summary>Reads the payment gateway API key from AWS Secrets Manager.</summary>
/// <remarks>
/// The secret is read on every checkout so each trace shows the call. A production service would
/// cache it (for example with the AWS Secrets Manager caching library) and refresh on rotation.
/// </remarks>
public sealed class SecretsManagerPaymentCredentialProvider(
    IAmazonSecretsManager secrets,
    IOptions<ShopOptions> options,
    ILogger<SecretsManagerPaymentCredentialProvider> logger) : IPaymentCredentialProvider
{
    public async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var secretId = options.Value.Integrations.PaymentSecretId;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await secrets.GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId }, ct);
            var apiKey = ParseApiKey(response.SecretString);
            ShopLog.PaymentCredentialsLoaded(logger, "secretsmanager", stopwatch.ElapsedMilliseconds);
            return apiKey;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ShopLog.PaymentCredentialsFailed(logger, ex, "secretsmanager");
            throw;
        }
    }

    // Accepts either a JSON secret {"apiKey":"..."} or a plain string.
    private static string ParseApiKey(string? secretString)
    {
        if (string.IsNullOrWhiteSpace(secretString))
            throw new InvalidOperationException("The payment secret is empty.");
        if (!secretString.TrimStart().StartsWith('{'))
            return secretString;

        using var doc = JsonDocument.Parse(secretString);
        return doc.RootElement.TryGetProperty("apiKey", out var key) && key.GetString() is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException("The payment secret has no \"apiKey\" field.");
    }
}

/// <summary>Used when no secret is configured (local runs without AWS).</summary>
public sealed class LocalPaymentCredentialProvider : IPaymentCredentialProvider
{
    public Task<string> GetApiKeyAsync(CancellationToken ct) => Task.FromResult("local-dev-key");
}

/// <summary>Stores an order receipt after the order is placed.</summary>
public interface IOrderReceiptStore
{
    Task StoreAsync(OrderDto order, CancellationToken ct);
}

/// <summary>Writes one JSON receipt per order to Amazon S3.</summary>
public sealed class S3OrderReceiptStore(
    IAmazonS3 s3,
    IOptions<ShopOptions> options,
    ILogger<S3OrderReceiptStore> logger) : IOrderReceiptStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task StoreAsync(OrderDto order, CancellationToken ct)
    {
        var bucket = options.Value.Integrations.ReceiptsBucket;
        var key = $"receipts/{order.CreatedAt:yyyy/MM/dd}/{order.OrderId}.json";
        var stopwatch = Stopwatch.StartNew();

        // No customer name, email, or address: the receipt carries only what fulfillment needs.
        var receipt = new
        {
            order.OrderId,
            order.SessionId,
            order.CreatedAt,
            order.Total,
            Items = order.Items.Select(i => new { i.ProductId, i.Name, i.UnitPrice, i.Quantity }),
        };

        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = "application/json",
            ContentBody = JsonSerializer.Serialize(receipt, Json),
        }, ct);

        ShopLog.ReceiptStored(logger, order.OrderId, $"s3://{bucket}/{key}", stopwatch.ElapsedMilliseconds);
    }
}

/// <summary>Used when no bucket is configured.</summary>
public sealed class NoOpOrderReceiptStore : IOrderReceiptStore
{
    public Task StoreAsync(OrderDto order, CancellationToken ct) => Task.CompletedTask;
}
