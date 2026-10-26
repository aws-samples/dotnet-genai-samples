using Microsoft.AspNetCore.Mvc;
using Shop.Contracts;
using Shop.Web.Models;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

public class CheckoutController(ShopApiClient api, ILogger<CheckoutController> logger, ILoggerFactory loggers) : Controller
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var cart = await api.GetCartAsync(HttpContext.GetShopSessionId(), ct);
        if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");
        ShopperJourney.StartedCheckout(_journey, ShopperJourney.ShortId(cart.SessionId), cart.Total);
        return View(new CheckoutViewModel { Cart = cart });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutViewModel model, CancellationToken ct)
    {
        var sessionId = HttpContext.GetShopSessionId();
        if (!ModelState.IsValid)
        {
            // Log which fields failed, never their values (they're personal data).
            var invalid = string.Join(", ", ModelState.Where(e => e.Value?.Errors.Count > 0).Select(e => e.Key));
            WebLog.CheckoutFormInvalid(logger, sessionId, invalid);
            model.Cart = await api.GetCartAsync(sessionId, ct);
            return View(model);
        }

        var (order, error) = await api.PlaceOrderAsync(
            new PlaceOrderRequest(sessionId, model.CustomerName, model.Email, model.ShippingAddress), ct);

        if (order is null)
        {
            WebLog.CheckoutRejected(logger, sessionId, error ?? "unknown");
            ShopperJourney.CheckoutFailed(_journey, ShopperJourney.ShortId(sessionId), error ?? "unknown error");
            ModelState.AddModelError(string.Empty, error ?? "Checkout failed.");
            model.Cart = await api.GetCartAsync(sessionId, ct);
            return View(model);
        }

        WebLog.OrderConfirmed(logger, order.OrderId, sessionId, order.Total);
        ShopperJourney.OrderPlaced(_journey, ShopperJourney.ShortId(sessionId), order.Total, order.OrderId);
        return RedirectToAction("Details", "Orders", new { id = order.OrderId });
    }
}
