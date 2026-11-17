using Microsoft.AspNetCore.Mvc;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

public class CartController(ShopApiClient api, ILoggerFactory loggers) : Controller
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    private string Shopper => ShopperJourney.ShortId(HttpContext.GetShopSessionId());

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await api.GetCartAsync(HttpContext.GetShopSessionId(), ct);
        ShopperJourney.ViewedCart(_journey, Shopper, cart.ItemCount, cart.Total);
        return View(cart);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string productId, int quantity, CancellationToken ct)
    {
        quantity = Math.Clamp(quantity, 1, 10);
        var cart = await api.AddToCartAsync(HttpContext.GetShopSessionId(), productId, quantity, ct);
        var name = cart.Items.FirstOrDefault(i => i.ProductId == productId)?.Name ?? productId;
        ShopperJourney.AddedToCart(_journey, Shopper, quantity, name, cart.ItemCount, cart.Total);
        TempData["Message"] = "Added to cart.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(string productId, CancellationToken ct)
    {
        var sessionId = HttpContext.GetShopSessionId();
        var before = await api.GetCartAsync(sessionId, ct);
        var name = before.Items.FirstOrDefault(i => i.ProductId == productId)?.Name ?? productId;
        var cart = await api.RemoveFromCartAsync(sessionId, productId, ct);
        ShopperJourney.RemovedFromCart(_journey, Shopper, name, cart.ItemCount, cart.Total);
        return RedirectToAction(nameof(Index));
    }
}
