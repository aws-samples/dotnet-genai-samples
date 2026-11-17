using Microsoft.AspNetCore.Mvc;
using Shop.Web.Services;

namespace Shop.Web.ViewComponents;

/// <summary>Cart badge in the nav bar. Fails soft so a down API doesn't break every page.</summary>
public class CartSummaryViewComponent(ShopApiClient api, ILogger<CartSummaryViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cart = await api.GetCartAsync(HttpContext.GetShopSessionId(), HttpContext.RequestAborted);
            return View(cart.ItemCount);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not load cart summary");
            return View(0);
        }
    }
}
