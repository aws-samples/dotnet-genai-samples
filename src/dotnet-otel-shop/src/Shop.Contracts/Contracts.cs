namespace Shop.Contracts;

public record ProductDto(
    string Id,
    string Name,
    string Description,
    string Category,
    decimal Price,
    string ImageUrl,
    int Stock);

public record CartItemDto(string ProductId, string Name, decimal UnitPrice, int Quantity)
{
    public decimal LineTotal => UnitPrice * Quantity;
}

public record CartDto(string SessionId, IReadOnlyList<CartItemDto> Items)
{
    public decimal Total => Items.Sum(i => i.LineTotal);
    public int ItemCount => Items.Sum(i => i.Quantity);
}

public record AddCartItemRequest(string ProductId, int Quantity);

public record PlaceOrderRequest(
    string SessionId,
    string CustomerName,
    string Email,
    string ShippingAddress);

public record OrderDto(
    string OrderId,
    string SessionId,
    string CustomerName,
    string Email,
    string ShippingAddress,
    IReadOnlyList<CartItemDto> Items,
    decimal Total,
    string Status,
    DateTimeOffset CreatedAt);

public static class OrderStatus
{
    public const string Placed = "Placed";
    public const string Fulfilled = "Fulfilled";
}

/// <summary>Result of a telemetry demo action, with ids to look the data up in AWS.</summary>
public record DemoResult(
    string Signal,
    string TraceId,
    string XRayTraceId,
    IReadOnlyList<string> WhatHappened,
    IReadOnlyList<string> WhereToLook);
