using Shop.Api.Data;
using Shop.Api.Diagnostics;
using Shop.Api.Services;
using Shop.Contracts;

namespace Shop.Api.Endpoints;

public static class ShopEndpoints
{
    public static void MapShopEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Loggers per area, so the "category" field in logs says Shop.Api.Catalog / Shop.Api.Cart.
        var loggers = app.Services.GetRequiredService<ILoggerFactory>();
        var catalogLog = loggers.CreateLogger("Shop.Api.Catalog");
        var cartLog = loggers.CreateLogger("Shop.Api.Cart");

        // Products
        api.MapGet("/products", async (string? category, IProductRepository products, CancellationToken ct) =>
        {
            var list = await products.ListAsync(category, ct);
            ShopLog.CatalogListed(catalogLog, list.Count, category ?? "all");
            return Results.Ok(list);
        });

        api.MapGet("/products/{id}", async (string id, IProductRepository products, CancellationToken ct) =>
        {
            var product = await products.GetAsync(id, ct);
            if (product is null)
            {
                ShopLog.ProductNotFound(catalogLog, id);
                return Results.NotFound();
            }

            ShopLog.ProductViewed(catalogLog, product.Id, product.Name, product.Price, product.Stock);
            return Results.Ok(product);
        });

        api.MapGet("/categories", async (IProductRepository products, CancellationToken ct) =>
            Results.Ok((await products.ListAsync(null, ct)).Select(p => p.Category).Distinct().Order()));

        // Carts
        api.MapGet("/carts/{sessionId}", async (string sessionId, ICartRepository carts, CancellationToken ct) =>
            Results.Ok(await carts.GetAsync(sessionId, ct)));

        api.MapPost("/carts/{sessionId}/items", async (
            string sessionId, AddCartItemRequest request,
            ICartRepository carts, IProductRepository products, CancellationToken ct) =>
        {
            if (request.Quantity is < 1 or > 10)
            {
                ShopLog.CartQuantityRejected(cartLog, sessionId, request.Quantity, request.ProductId);
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["quantity"] = ["Quantity must be 1-10."] });
            }

            var product = await products.GetAsync(request.ProductId, ct);
            if (product is null)
            {
                ShopLog.ProductNotFound(catalogLog, request.ProductId);
                return Results.NotFound();
            }

            var cart = await carts.GetAsync(sessionId, ct);
            var items = cart.Items.ToList();
            var existing = items.FindIndex(i => i.ProductId == product.Id);
            if (existing >= 0)
                items[existing] = items[existing] with { Quantity = Math.Min(items[existing].Quantity + request.Quantity, 10) };
            else
                items.Add(new CartItemDto(product.Id, product.Name, product.Price, request.Quantity));

            var updated = cart with { Items = items };
            await carts.SaveAsync(updated, ct);
            ShopDiagnostics.CartItemsAdded.Add(request.Quantity,
                new KeyValuePair<string, object?>("product.category", product.Category));
            ShopLog.CartItemAdded(cartLog, sessionId, request.Quantity, product.Id, product.Name, updated.ItemCount, updated.Total);
            return Results.Ok(updated);
        });

        api.MapDelete("/carts/{sessionId}/items/{productId}", async (
            string sessionId, string productId, ICartRepository carts, CancellationToken ct) =>
        {
            var cart = await carts.GetAsync(sessionId, ct);
            var updated = cart with { Items = cart.Items.Where(i => i.ProductId != productId).ToList() };
            await carts.SaveAsync(updated, ct);
            ShopLog.CartItemRemoved(cartLog, sessionId, productId, updated.ItemCount, updated.Total);
            return Results.Ok(updated);
        });

        // Orders
        api.MapPost("/orders", async (PlaceOrderRequest request, CheckoutService checkout, CancellationToken ct) =>
        {
            try
            {
                var order = await checkout.PlaceOrderAsync(request, ct);
                return Results.Created($"/api/orders/{order.OrderId}", order);
            }
            catch (CheckoutException ex)
            {
                var status = ex.Reason == "payment_declined" ? StatusCodes.Status402PaymentRequired : StatusCodes.Status409Conflict;
                return Results.Problem(title: ex.Message, statusCode: status, extensions: new Dictionary<string, object?> { ["reason"] = ex.Reason });
            }
        });

        api.MapGet("/orders/{orderId}", async (string orderId, IOrderRepository orders, CancellationToken ct) =>
            await orders.GetAsync(orderId, ct) is { } order ? Results.Ok(order) : Results.NotFound());

        api.MapGet("/orders", async (string sessionId, IOrderRepository orders, CancellationToken ct) =>
            Results.Ok(await orders.ListBySessionAsync(sessionId, ct)));
    }
}
