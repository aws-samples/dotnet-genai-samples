using System.Diagnostics;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Diagnostics;
using Shop.Api.Integrations;
using Shop.Api.Messaging;
using Shop.Contracts;

namespace Shop.Api.Services;

public class CheckoutException(string reason, string message) : Exception(message)
{
    public string Reason { get; } = reason;
}

public class CheckoutService(
    ICartRepository carts,
    IProductRepository products,
    IOrderRepository orders,
    IOrderEventPublisher events,
    SecretsManagerPaymentCredentialProvider paymentCredentials,
    S3OrderReceiptStore receipts,
    IOptions<ShopOptions> options,
    ILogger<CheckoutService> logger)
{
    public async Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("Checkout");
        activity?.SetTag("shop.session.id", request.SessionId);
        var stopwatch = Stopwatch.StartNew();
        var outcome = "failure";   // set to "success" only once the order is saved and published

        try
        {
            var cart = await carts.GetAsync(request.SessionId, ct);
            if (cart.Items.Count == 0)
                throw new CheckoutException("empty_cart", "Your cart is empty.");

            ShopLog.CheckoutStarted(logger, request.SessionId, cart.ItemCount, cart.Total);
            await ValidateStockAsync(request.SessionId, cart, ct);
            await ProcessPaymentAsync(cart.Total, ct);

            var order = new OrderDto(
                OrderId: $"ord-{Guid.NewGuid():N}"[..16],
                SessionId: request.SessionId,
                CustomerName: request.CustomerName,
                Email: request.Email,
                ShippingAddress: request.ShippingAddress,
                Items: cart.Items,
                Total: cart.Total,
                Status: OrderStatus.Placed,
                CreatedAt: DateTimeOffset.UtcNow);

            await orders.CreateAsync(order, ct);
            await carts.DeleteAsync(request.SessionId, ct);
            await events.PublishAsync(new OrderPlacedEvent(order.OrderId, order.SessionId, order.Total, cart.ItemCount, order.CreatedAt), ct);
            await StoreReceiptAsync(order, ct);

            activity?.SetTag("order.id", order.OrderId);
            activity?.SetTag("order.total", (double)order.Total);
            activity?.SetTag("order.item_count", cart.ItemCount);
            ShopDiagnostics.OrdersPlaced.Add(1);
            ShopDiagnostics.OrderValue.Record((double)order.Total);
            ShopDiagnostics.OrderRevenue.Add((double)order.Total);
            outcome = "success";
            ShopLog.OrderPlaced(logger, order.OrderId, order.SessionId, cart.ItemCount, order.Total, stopwatch.ElapsedMilliseconds);

            return order;
        }
        catch (CheckoutException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("checkout.failure_reason", ex.Reason);
            ShopDiagnostics.CheckoutFailures.Add(1, new KeyValuePair<string, object?>("reason", ex.Reason));
            ShopLog.CheckoutFailed(logger, request.SessionId, ex.Reason, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            // Every attempt counts, including unexpected exceptions, so failures can't hide.
            var outcomeTag = new KeyValuePair<string, object?>("outcome", outcome);
            ShopDiagnostics.OrdersProcessed.Add(1, outcomeTag);
            ShopDiagnostics.TotalProcessingDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds, outcomeTag);
        }
    }

    private async Task ValidateStockAsync(string sessionId, CartDto cart, CancellationToken ct)
    {
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("ValidateStock");
        foreach (var item in cart.Items)
        {
            var product = await products.GetAsync(item.ProductId, ct);
            if (product is null || product.Stock < item.Quantity)
            {
                ShopLog.OutOfStock(logger, sessionId, item.ProductId, item.Quantity, product?.Stock ?? 0);
                throw new CheckoutException("out_of_stock", $"{item.Name} is out of stock.");
            }
        }
    }

    /// <summary>
    /// Writes the receipt to S3. A receipt failure is recorded on the span and logged, but it
    /// doesn't fail an order that is already saved and published.
    /// </summary>
    private async Task StoreReceiptAsync(OrderDto order, CancellationToken ct)
    {
        try
        {
            await receipts.StoreAsync(order, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Activity.Current?.AddException(ex);
            ShopLog.ReceiptFailed(logger, ex, order.OrderId);
        }
    }

    /// <summary>Simulated payment gateway with configurable latency and failure rate.</summary>
    private async Task ProcessPaymentAsync(decimal amount, CancellationToken ct)
    {
        var chaos = options.Value.Chaos;
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("ProcessPayment", ActivityKind.Client);
        activity?.SetTag("payment.amount", (double)amount);
        activity?.SetTag("payment.provider", "fake-pay");
        // Names the remote side of this client span. Application Signals and the X-Ray service map
        // show it as the "fake-pay" dependency instead of UnknownRemoteService.
        activity?.SetTag("peer.service", "fake-pay");
        var stopwatch = Stopwatch.StartNew();

        // The gateway key lives in Secrets Manager; the lookup shows up as a child span of ProcessPayment.
        string apiKey;
        try
        {
            apiKey = await paymentCredentials.GetApiKeyAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, "payment_unavailable");
            throw new CheckoutException("payment_unavailable", "Payments are temporarily unavailable. Please try again.");
        }
        activity?.SetTag("payment.credentials_loaded", apiKey.Length > 0);

        await Task.Delay(Random.Shared.Next(chaos.PaymentMinLatencyMs, chaos.PaymentMaxLatencyMs + 1), ct);

        if (Random.Shared.NextDouble() < chaos.PaymentFailureRate)
        {
            ShopLog.PaymentDeclined(logger, amount, stopwatch.ElapsedMilliseconds, "fake-pay");
            var ex = new CheckoutException("payment_declined", "Payment was declined. Please try again.");
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error, "payment_declined");
            throw ex;
        }

        ShopLog.PaymentAuthorized(logger, amount, stopwatch.ElapsedMilliseconds, "fake-pay");
    }
}
