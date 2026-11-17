using System.Net;
using Microsoft.AspNetCore.Mvc;
using Shop.Contracts;

namespace Shop.Web.Services;

/// <summary>
/// Typed client for the Shop REST API. HttpClient instrumentation creates a
/// client span per call and propagates the trace context to the API.
/// </summary>
public class ShopApiClient(HttpClient http)
{
    public async Task<IReadOnlyList<ProductDto>> GetProductsAsync(string? category = null, CancellationToken ct = default)
    {
        var url = category is null ? "api/products" : $"api/products?category={Uri.EscapeDataString(category)}";
        return await http.GetFromJsonAsync<List<ProductDto>>(url, ct) ?? [];
    }

    public async Task<ProductDto?> GetProductAsync(string id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/products/{Uri.EscapeDataString(id)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(ct);
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<string>>("api/categories", ct) ?? [];

    public async Task<CartDto> GetCartAsync(string sessionId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<CartDto>($"api/carts/{sessionId}", ct) ?? new CartDto(sessionId, []);

    public async Task<CartDto> AddToCartAsync(string sessionId, string productId, int quantity, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"api/carts/{sessionId}/items", new AddCartItemRequest(productId, quantity), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartDto>(ct) ?? new CartDto(sessionId, []);
    }

    public async Task<CartDto> RemoveFromCartAsync(string sessionId, string productId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"api/carts/{sessionId}/items/{Uri.EscapeDataString(productId)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CartDto>(ct) ?? new CartDto(sessionId, []);
    }

    /// <summary>Returns the order, or the API's error message when checkout is rejected (e.g. payment declined).</summary>
    public async Task<(OrderDto? Order, string? Error)> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/orders", request, ct);
        if (response.IsSuccessStatusCode)
            return (await response.Content.ReadFromJsonAsync<OrderDto>(ct), null);

        if (response.StatusCode is HttpStatusCode.PaymentRequired or HttpStatusCode.Conflict)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
            return (null, problem?.Title ?? "Checkout failed.");
        }

        response.EnsureSuccessStatusCode();
        return (null, "Checkout failed.");
    }

    public async Task<OrderDto?> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/orders/{Uri.EscapeDataString(orderId)}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderDto>(ct);
    }

    public async Task<IReadOnlyList<OrderDto>> GetOrdersAsync(string sessionId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<List<OrderDto>>($"api/orders?sessionId={sessionId}", ct) ?? [];

    /// <summary>Runs a telemetry demo action. The "error" demo returns HTTP 500 on purpose.</summary>
    public async Task<DemoResult?> RunDemoAsync(string signal, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"api/demo/{Uri.EscapeDataString(signal)}", content: null, ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        return await response.Content.ReadFromJsonAsync<DemoResult>(ct);
    }
}
