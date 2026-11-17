using System.Collections.Concurrent;
using Shop.Contracts;

namespace Shop.Api.Data;

// Local-only implementations so the app runs without AWS credentials.

public class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<string, ProductDto> _products = new();

    public Task<IReadOnlyList<ProductDto>> ListAsync(string? category, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ProductDto>>(_products.Values
            .Where(p => category is null || p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .ToList());

    public Task<ProductDto?> GetAsync(string id, CancellationToken ct) =>
        Task.FromResult(_products.GetValueOrDefault(id));

    public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(!_products.IsEmpty);

    public Task UpsertAsync(ProductDto product, CancellationToken ct)
    {
        _products[product.Id] = product;
        return Task.CompletedTask;
    }
}

public class InMemoryCartRepository : ICartRepository
{
    private readonly ConcurrentDictionary<string, CartDto> _carts = new();

    public Task<CartDto> GetAsync(string sessionId, CancellationToken ct) =>
        Task.FromResult(_carts.GetValueOrDefault(sessionId) ?? new CartDto(sessionId, []));

    public Task SaveAsync(CartDto cart, CancellationToken ct)
    {
        _carts[cart.SessionId] = cart;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string sessionId, CancellationToken ct)
    {
        _carts.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<string, OrderDto> _orders = new();

    public Task CreateAsync(OrderDto order, CancellationToken ct)
    {
        _orders[order.OrderId] = order;
        return Task.CompletedTask;
    }

    public Task<OrderDto?> GetAsync(string orderId, CancellationToken ct) =>
        Task.FromResult(_orders.GetValueOrDefault(orderId));

    public Task<IReadOnlyList<OrderDto>> ListBySessionAsync(string sessionId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<OrderDto>>(_orders.Values
            .Where(o => o.SessionId == sessionId)
            .OrderByDescending(o => o.CreatedAt)
            .ToList());

    public Task UpdateStatusAsync(string orderId, string status, CancellationToken ct)
    {
        if (_orders.TryGetValue(orderId, out var order))
            _orders[orderId] = order with { Status = status };
        return Task.CompletedTask;
    }
}
