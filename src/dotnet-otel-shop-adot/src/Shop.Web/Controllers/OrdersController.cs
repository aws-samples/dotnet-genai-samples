using Microsoft.AspNetCore.Mvc;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

public class OrdersController(ShopApiClient api, ILoggerFactory loggers) : Controller
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    public async Task<IActionResult> Index(CancellationToken ct) =>
        View(await api.GetOrdersAsync(HttpContext.GetShopSessionId(), ct));

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var order = await api.GetOrderAsync(id, ct);
        // Only show orders that belong to this shopper's session.
        if (order is null || order.SessionId != HttpContext.GetShopSessionId()) return NotFound();
        ShopperJourney.CheckedOrder(_journey, ShopperJourney.ShortId(order.SessionId), order.OrderId, order.Status);
        return View(order);
    }
}
