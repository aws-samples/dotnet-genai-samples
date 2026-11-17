namespace Shop.Web.Services;

/// <summary>
/// Plain-English log of what each shopper does, for demos:
///   "Shopper 6f3a7e added 2 x Trail Runner Shoes to the cart. Cart: 2 items, $179.98"
///
/// Uses the "ShopperJourney" log category, which also goes to its own file
/// (Logging:Journey:Path, default /tmp/shop-logs/shopper-journey.log).
/// Shoppers are shown by the first 6 characters of their anonymous session id.
/// </summary>
public static partial class ShopperJourney
{
    public const string Category = "ShopperJourney";

    public static ILogger CreateLogger(ILoggerFactory factory) => factory.CreateLogger(Category);

    public static string ShortId(string sessionId) => sessionId.Length > 6 ? sessionId[..6] : sessionId;

    [LoggerMessage(7001, LogLevel.Information, "Shopper {Shopper} opened the home page", EventName = "JourneyHome")]
    public static partial void OpenedHome(ILogger logger, string shopper);

    [LoggerMessage(7002, LogLevel.Information, "Shopper {Shopper} browsed {Category} ({ProductCount} products)", EventName = "JourneyBrowse")]
    public static partial void Browsed(ILogger logger, string shopper, string category, int productCount);

    [LoggerMessage(7003, LogLevel.Information, "Shopper {Shopper} clicked on {ProductName} (${Price:0.00})", EventName = "JourneyProductClick")]
    public static partial void ClickedProduct(ILogger logger, string shopper, string productName, decimal price);

    [LoggerMessage(7004, LogLevel.Information, "Shopper {Shopper} added {Quantity} x {ProductName} to the cart. Cart: {CartItems} items, ${CartTotal:0.00}", EventName = "JourneyAddToCart")]
    public static partial void AddedToCart(ILogger logger, string shopper, int quantity, string productName, int cartItems, decimal cartTotal);

    [LoggerMessage(7005, LogLevel.Information, "Shopper {Shopper} removed {ProductName} from the cart. Cart: {CartItems} items, ${CartTotal:0.00}", EventName = "JourneyRemoveFromCart")]
    public static partial void RemovedFromCart(ILogger logger, string shopper, string productName, int cartItems, decimal cartTotal);

    [LoggerMessage(7006, LogLevel.Information, "Shopper {Shopper} opened the cart: {CartItems} items, ${CartTotal:0.00}", EventName = "JourneyViewCart")]
    public static partial void ViewedCart(ILogger logger, string shopper, int cartItems, decimal cartTotal);

    [LoggerMessage(7007, LogLevel.Information, "Shopper {Shopper} started checkout for ${CartTotal:0.00}", EventName = "JourneyCheckoutStarted")]
    public static partial void StartedCheckout(ILogger logger, string shopper, decimal cartTotal);

    [LoggerMessage(7008, LogLevel.Information, "Shopper {Shopper} paid ${OrderTotal:0.00}. Order {OrderId} confirmed", EventName = "JourneyOrderPlaced")]
    public static partial void OrderPlaced(ILogger logger, string shopper, decimal orderTotal, string orderId);

    [LoggerMessage(7009, LogLevel.Warning, "Shopper {Shopper} could not check out: {Reason}", EventName = "JourneyCheckoutFailed")]
    public static partial void CheckoutFailed(ILogger logger, string shopper, string reason);

    [LoggerMessage(7010, LogLevel.Information, "Shopper {Shopper} checked order {OrderId} (status: {Status})", EventName = "JourneyOrderChecked")]
    public static partial void CheckedOrder(ILogger logger, string shopper, string orderId, string status);

    [LoggerMessage(7011, LogLevel.Information, "Shopper {Shopper} left without buying ({CartItems} items, ${CartTotal:0.00} in the cart)", EventName = "JourneyAbandoned")]
    public static partial void Abandoned(ILogger logger, string shopper, int cartItems, decimal cartTotal);
}
