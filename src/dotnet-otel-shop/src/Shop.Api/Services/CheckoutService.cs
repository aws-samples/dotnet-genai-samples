using System.Diagnostics;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Api.Diagnostics;
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
    IOptions<ShopOptions> options,
    ILogger<CheckoutService> logger)
{
    public async Task<OrderDto> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct)
    {
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("Checkout");
        activity?.SetTag("shop.session.id", request.SessionId);
        var stopwatch = Stopwatch.StartNew();

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

            activity?.SetTag("order.id", order.OrderId);
            activity?.SetTag("order.total", (double)order.Total);
            activity?.SetTag("order.item_count", cart.ItemCount);
            ShopDiagnostics.OrdersPlaced.Add(1);
            ShopDiagnostics.OrderValue.Record((double)order.Total);
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

    /// <summary>Simulated payment gateway with configurable latency and failure rate.</summary>
    private async Task ProcessPaymentAsync(decimal amount, CancellationToken ct)
    {
        var chaos = options.Value.Chaos;
        using var activity = ShopDiagnostics.ActivitySource.StartActivity("ProcessPayment", ActivityKind.Client);
        activity?.SetTag("payment.amount", (double)amount);
        activity?.SetTag("payment.provider", "fake-pay");
        var stopwatch = Stopwatch.StartNew();

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
