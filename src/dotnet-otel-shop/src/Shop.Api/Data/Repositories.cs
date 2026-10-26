using Shop.Contracts;

namespace Shop.Api.Data;

public interface IProductRepository
{
    Task<IReadOnlyList<ProductDto>> ListAsync(string? category, CancellationToken ct);
    Task<ProductDto?> GetAsync(string id, CancellationToken ct);
    Task<bool> AnyAsync(CancellationToken ct);
    Task UpsertAsync(ProductDto product, CancellationToken ct);
}

public interface ICartRepository
{
    Task<CartDto> GetAsync(string sessionId, CancellationToken ct);
    Task SaveAsync(CartDto cart, CancellationToken ct);
    Task DeleteAsync(string sessionId, CancellationToken ct);
}

public interface IOrderRepository
{
    Task CreateAsync(OrderDto order, CancellationToken ct);
    Task<OrderDto?> GetAsync(string orderId, CancellationToken ct);
    Task<IReadOnlyList<OrderDto>> ListBySessionAsync(string sessionId, CancellationToken ct);
    Task UpdateStatusAsync(string orderId, string status, CancellationToken ct);
}
