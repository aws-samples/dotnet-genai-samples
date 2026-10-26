using System.ComponentModel.DataAnnotations;
using Shop.Contracts;

namespace Shop.Web.Models;

public record HomeViewModel(IReadOnlyList<ProductDto> Featured, IReadOnlyList<string> Categories);

public record CatalogViewModel(IReadOnlyList<ProductDto> Products, IReadOnlyList<string> Categories, string? SelectedCategory);

public class CheckoutViewModel
{
    public CartDto? Cart { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string CustomerName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(300)]
    [Display(Name = "Shipping address")]
    public string ShippingAddress { get; set; } = "";
}

public class ErrorViewModel
{
    public string? TraceId { get; set; }
    public string? XRayTraceId { get; set; }
}
