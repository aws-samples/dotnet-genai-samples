using System.Diagnostics;
using Shop.Contracts;

namespace Shop.Web.Services;

public record SimulationSummary(int Shoppers, int Orders, int Declined, int Abandoned, int Errors, long DurationMs);

/// <summary>
/// Generates realistic traffic for demos: each simulated shopper browses, views products,
/// fills a cart, sometimes removes an item, and usually checks out. Every shopper is its
/// own trace (root span "SimulatedShopper"), so X-Ray shows many separate journeys.
/// </summary>
public class ShopperSimulator(ShopApiClient api, ILogger<ShopperSimulator> logger, ILoggerFactory loggers)
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    public const string SourceName = "Shop.Web.Simulator";
    private static readonly ActivitySource ActivitySource = new(SourceName);

    public async Task<SimulationSummary> RunAsync(int shoppers, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        using var throttle = new SemaphoreSlim(5); // at most 5 shoppers at once

        var outcomes = await Task.WhenAll(Enumerable.Range(0, shoppers).Select(async _ =>
        {
            await throttle.WaitAsync(ct);
            try { return await Task.Run(() => RunShopperAsync(ct), ct); }
            finally { throttle.Release(); }
        }));

        var summary = new SimulationSummary(
            shoppers,
            Orders: outcomes.Count(o => o == "ordered"),
            Declined: outcomes.Count(o => o == "declined"),
            Abandoned: outcomes.Count(o => o == "abandoned"),
            Errors: outcomes.Count(o => o == "error"),
            DurationMs: stopwatch.ElapsedMilliseconds);

        WebLog.SimulationFinished(logger, summary.Shoppers, summary.Orders, summary.Declined, summary.Abandoned, summary.DurationMs);
        return summary;
    }

    private async Task<string> RunShopperAsync(CancellationToken ct)
    {
        // Detach from the button-click request so each shopper starts a new trace.
        Activity.Current = null;
        var sessionId = Guid.NewGuid().ToString("N");
        using var activity = ActivitySource.StartActivity("SimulatedShopper");
        activity?.SetTag("shop.session.id", sessionId);
        activity?.SetTag("shop.simulated", true);

        var stopwatch = Stopwatch.StartNew();
        var shopper = ShopperJourney.ShortId(sessionId);
        int viewed = 0, added = 0;
        string outcome;
        try
        {
            // Browse a category (or everything).
            var categories = await api.GetCategoriesAsync(ct);
            var category = Random.Shared.Next(3) == 0 ? null : categories[Random.Shared.Next(categories.Count)];
            ShopperJourney.OpenedHome(_journey, shopper);
            var products = await api.GetProductsAsync(category, ct);
            if (products.Count == 0) products = await api.GetProductsAsync(null, ct);
            ShopperJourney.Browsed(_journey, shopper, category ?? "all products", products.Count);

            // Look at a few products, pausing like a person would.
            var picks = products.OrderBy(_ => Random.Shared.Next()).Take(Random.Shared.Next(1, 4)).ToList();
            foreach (var product in picks)
            {
                await api.GetProductAsync(product.Id, ct);
                ShopperJourney.ClickedProduct(_journey, shopper, product.Name, product.Price);
                viewed++;
                await Task.Delay(Random.Shared.Next(50, 200), ct);
            }

            // Add some of them to the cart.
            var inCart = picks.Take(Random.Shared.Next(1, picks.Count + 1)).ToList();
            foreach (var product in inCart)
            {
                var quantity = Random.Shared.Next(1, 4);
                var cart = await api.AddToCartAsync(sessionId, product.Id, quantity, ct);
                ShopperJourney.AddedToCart(_journey, shopper, quantity, product.Name, cart.ItemCount, cart.Total);
                added += quantity;
            }

            // Some shoppers change their mind about one item (never their only item).
            if (inCart.Count > 1 && Random.Shared.NextDouble() < 0.2)
            {
                var cart = await api.RemoveFromCartAsync(sessionId, inCart[0].Id, ct);
                ShopperJourney.RemovedFromCart(_journey, shopper, inCart[0].Name, cart.ItemCount, cart.Total);
            }

            // Most check out; the rest abandon their cart.
            var current = await api.GetCartAsync(sessionId, ct);
            if (Random.Shared.NextDouble() < 0.75)
            {
                ShopperJourney.StartedCheckout(_journey, shopper, current.Total);
                var (order, error) = await api.PlaceOrderAsync(
                    new PlaceOrderRequest(sessionId, "Simulated Shopper", "shopper@example.com", "1 Demo Street"), ct);
                if (order is null)
                {
                    ShopperJourney.CheckoutFailed(_journey, shopper, error ?? "unknown error");
                    outcome = "declined";
                }
                else
                {
                    ShopperJourney.OrderPlaced(_journey, shopper, order.Total, order.OrderId);
                    outcome = "ordered";
                }
            }
            else
            {
                ShopperJourney.Abandoned(_journey, shopper, current.ItemCount, current.Total);
                outcome = "abandoned";
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);
            WebLog.SimulatedShopperFailed(logger, ex, sessionId);
            outcome = "error";
        }

        activity?.SetTag("shop.outcome", outcome);
        WebLog.SimulatedShopperFinished(logger, sessionId, outcome, viewed, added, stopwatch.ElapsedMilliseconds);
        return outcome;
    }
}
