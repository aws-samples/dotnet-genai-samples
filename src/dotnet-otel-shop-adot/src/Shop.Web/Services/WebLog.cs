namespace Shop.Web.Services;

/// <summary>Storefront events, as source-generated log methods. Query by EventId.Name in Logs Insights.</summary>
public static partial class WebLog
{
    [LoggerMessage(5001, LogLevel.Warning, "Checkout form invalid for {SessionId}: {InvalidFields}", EventName = "CheckoutFormInvalid")]
    public static partial void CheckoutFormInvalid(ILogger logger, string sessionId, string invalidFields);

    [LoggerMessage(5002, LogLevel.Warning, "Checkout rejected for {SessionId}: {Reason}", EventName = "CheckoutRejected")]
    public static partial void CheckoutRejected(ILogger logger, string sessionId, string reason);

    [LoggerMessage(5003, LogLevel.Information, "Order {OrderId} confirmed to shopper {SessionId}: total {OrderTotal}", EventName = "OrderConfirmed")]
    public static partial void OrderConfirmed(ILogger logger, string orderId, string sessionId, decimal orderTotal);

    [LoggerMessage(6001, LogLevel.Information, "Simulated shopper {SessionId} finished: {Outcome} ({ProductsViewed} viewed, {ItemsAdded} added) in {DurationMs} ms", EventName = "SimulatedShopperFinished")]
    public static partial void SimulatedShopperFinished(ILogger logger, string sessionId, string outcome, int productsViewed, int itemsAdded, long durationMs);

    [LoggerMessage(6002, LogLevel.Information, "Simulation finished: {Shoppers} shoppers, {Orders} orders, {Declined} declined, {Abandoned} abandoned carts in {DurationMs} ms", EventName = "SimulationFinished")]
    public static partial void SimulationFinished(ILogger logger, int shoppers, int orders, int declined, int abandoned, long durationMs);

    [LoggerMessage(6003, LogLevel.Error, "Simulated shopper {SessionId} hit an error", EventName = "SimulatedShopperFailed")]
    public static partial void SimulatedShopperFailed(ILogger logger, Exception exception, string sessionId);
}
