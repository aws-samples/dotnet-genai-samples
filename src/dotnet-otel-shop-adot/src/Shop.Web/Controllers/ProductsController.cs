using Microsoft.AspNetCore.Mvc;
using Shop.Web.Models;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

public class ProductsController(ShopApiClient api, ILoggerFactory loggers) : Controller
{
    private readonly ILogger _journey = ShopperJourney.CreateLogger(loggers);

    private string Shopper => ShopperJourney.ShortId(HttpContext.GetShopSessionId());

    public async Task<IActionResult> Index(string? category, CancellationToken ct)
    {
        var products = await api.GetProductsAsync(category, ct);
        var categories = await api.GetCategoriesAsync(ct);
        ShopperJourney.Browsed(_journey, Shopper, category ?? "all products", products.Count);
        return View(new CatalogViewModel(products, categories, category));
    }

    public async Task<IActionResult> Details(string id, CancellationToken ct)
    {
        var product = await api.GetProductAsync(id, ct);
        if (product is null) return NotFound();
        ShopperJourney.ClickedProduct(_journey, Shopper, product.Name, product.Price);
        return View(product);
    }
}
