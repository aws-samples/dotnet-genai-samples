using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shop.Api.Diagnostics;

/// <summary>Custom spans and business metrics for the Shop API.</summary>
public static class ShopDiagnostics
{
    public const string SourceName = "Shop.Api";

    public static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> CartItemsAdded =
        Meter.CreateCounter<long>("shop.cart.items_added", unit: "{item}", description: "Items added to carts");

    public static readonly Counter<long> OrdersPlaced =
        Meter.CreateCounter<long>("shop.orders.placed", unit: "{order}", description: "Orders successfully placed");

    public static readonly Counter<long> CheckoutFailures =
        Meter.CreateCounter<long>("shop.checkout.failures", unit: "{failure}", description: "Failed checkouts by reason");

    public static readonly Histogram<double> OrderValue =
        Meter.CreateHistogram<double>("shop.order.value", unit: "USD", description: "Order totals");

    public static readonly Counter<long> OrdersFulfilled =
        Meter.CreateCounter<long>("shop.orders.fulfilled", unit: "{order}", description: "Orders fulfilled by the worker");

    // --- Order processing metrics -------------------------------------------------------------
    // Attribute values are kept to a small fixed set (for example success/failure). Never put an order or session id on a metric: each unique value becomes a separate
    // CloudWatch metric. Ids belong on spans.

    /// <summary>Every checkout attempt. Attribute: outcome = success | failure.</summary>
    public static readonly Counter<long> OrdersProcessed =
        Meter.CreateCounter<long>("OrdersProcessed", unit: "{order}", description: "Checkout attempts, by outcome");

    /// <summary>End-to-end checkout time: cart lookup, stock check, payment, save, publish.</summary>
    public static readonly Histogram<double> TotalProcessingDurationMs =
        Meter.CreateHistogram<double>("TotalProcessingDurationMs", unit: "ms", description: "Checkout processing time, by outcome");

    /// <summary>Revenue from successfully placed orders (sum of order totals).</summary>
    public static readonly Counter<double> OrderRevenue =
        Meter.CreateCounter<double>("OrderRevenue", unit: "USD", description: "Revenue from placed orders");
}
