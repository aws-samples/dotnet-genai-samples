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
}
