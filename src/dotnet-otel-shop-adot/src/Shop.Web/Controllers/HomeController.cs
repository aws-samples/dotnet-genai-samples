using Microsoft.AspNetCore.Mvc;
using Shop.Web.Models;
using Shop.Telemetry;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

public class HomeController(ShopApiClient api, ILoggerFactory loggers) : Controller
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var products = await api.GetProductsAsync(ct: ct);
        var categories = await api.GetCategoriesAsync(ct);
        ShopperJourney.OpenedHome(_journey, ShopperJourney.ShortId(HttpContext.GetShopSessionId()));
        return View(new HomeViewModel(products.Take(4).ToList(), categories));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel
        {
            TraceId = TraceIds.Current ?? HttpContext.TraceIdentifier,
            XRayTraceId = TraceIds.CurrentXRay,
        });
}
