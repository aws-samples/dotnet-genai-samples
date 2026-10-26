namespace Shop.Api.Diagnostics;

/// <summary>
/// Business events for the Shop API, as source-generated log methods.
///
/// Each event has a stable EventId and EventName, so in CloudWatch Logs Insights you can
/// filter or count by event:  stats count(*) by EventId.Name
/// Never log customer names, emails, or addresses here; the anonymous SessionId is enough
/// to follow a shopper.
/// </summary>
public static partial class ShopLog
{
    // Catalog: 1000s
    [LoggerMessage(1001, LogLevel.Information, "Catalog listed: {ProductCount} products (category {Category})", EventName = "CatalogListed")]
    public static partial void CatalogListed(ILogger logger, int productCount, string category);

    [LoggerMessage(1002, LogLevel.Information, "Product {ProductId} viewed: {ProductName} at {Price} ({Stock} in stock)", EventName = "ProductViewed")]
    public static partial void ProductViewed(ILogger logger, string productId, string productName, decimal price, int stock);

    [LoggerMessage(1003, LogLevel.Warning, "Product {ProductId} not found", EventName = "ProductNotFound")]
    public static partial void ProductNotFound(ILogger logger, string productId);

    // Cart: 2000s
    [LoggerMessage(2001, LogLevel.Information, "Cart {SessionId}: added {Quantity} x {ProductId} ({ProductName}); cart now {CartItemCount} items, total {CartTotal}", EventName = "CartItemAdded")]
    public static partial void CartItemAdded(ILogger logger, string sessionId, int quantity, string productId, string productName, int cartItemCount, decimal cartTotal);

    [LoggerMessage(2002, LogLevel.Information, "Cart {SessionId}: removed {ProductId}; cart now {CartItemCount} items, total {CartTotal}", EventName = "CartItemRemoved")]
    public static partial void CartItemRemoved(ILogger logger, string sessionId, string productId, int cartItemCount, decimal cartTotal);

    [LoggerMessage(2003, LogLevel.Warning, "Cart {SessionId}: rejected quantity {Quantity} for {ProductId}", EventName = "CartQuantityRejected")]
    public static partial void CartQuantityRejected(ILogger logger, string sessionId, int quantity, string productId);

    // Checkout: 3000s
    [LoggerMessage(3001, LogLevel.Information, "Checkout started for {SessionId}: {ItemCount} items, total {CartTotal}", EventName = "CheckoutStarted")]
    public static partial void CheckoutStarted(ILogger logger, string sessionId, int itemCount, decimal cartTotal);

    [LoggerMessage(3002, LogLevel.Warning, "Checkout for {SessionId}: {ProductId} out of stock (requested {Requested}, available {Available})", EventName = "OutOfStock")]
    public static partial void OutOfStock(ILogger logger, string sessionId, string productId, int requested, int available);

    [LoggerMessage(3003, LogLevel.Information, "Payment authorized: {Amount} in {LatencyMs} ms via {Provider}", EventName = "PaymentAuthorized")]
    public static partial void PaymentAuthorized(ILogger logger, decimal amount, long latencyMs, string provider);

    [LoggerMessage(3004, LogLevel.Warning, "Payment declined: {Amount} after {LatencyMs} ms via {Provider}", EventName = "PaymentDeclined")]
    public static partial void PaymentDeclined(ILogger logger, decimal amount, long latencyMs, string provider);

    [LoggerMessage(3005, LogLevel.Information, "Order {OrderId} placed for {SessionId}: {ItemCount} items, total {OrderTotal}, checkout took {CheckoutMs} ms", EventName = "OrderPlaced")]
    public static partial void OrderPlaced(ILogger logger, string orderId, string sessionId, int itemCount, decimal orderTotal, long checkoutMs);

    [LoggerMessage(3006, LogLevel.Warning, "Checkout failed for {SessionId}: {Reason} after {CheckoutMs} ms", EventName = "CheckoutFailed")]
    public static partial void CheckoutFailed(ILogger logger, string sessionId, string reason, long checkoutMs);

    // Fulfillment: 4000s
    [LoggerMessage(4001, LogLevel.Information, "Order {OrderId} event received from {MessagingSystem} after {QueueLatencyMs} ms in queue", EventName = "OrderEventReceived")]
    public static partial void OrderEventReceived(ILogger logger, string orderId, string messagingSystem, long queueLatencyMs);

    [LoggerMessage(4002, LogLevel.Information, "Order {OrderId} fulfilled: {ItemCount} items, total {OrderTotal}, {FulfillmentMs} ms after it was placed", EventName = "OrderFulfilled")]
    public static partial void OrderFulfilled(ILogger logger, string orderId, int itemCount, decimal orderTotal, long fulfillmentMs);

    [LoggerMessage(4004, LogLevel.Information, "Warehouse shipped order {OrderId} for shopper {Shopper} ({ItemCount} items, ${OrderTotal:0.00})", EventName = "JourneyOrderShipped")]
    public static partial void OrderShipped(ILogger logger, string orderId, string shopper, int itemCount, decimal orderTotal);

    [LoggerMessage(4003, LogLevel.Error, "Order {OrderId} fulfillment failed", EventName = "FulfillmentFailed")]
    public static partial void FulfillmentFailed(ILogger logger, Exception exception, string orderId);
}
