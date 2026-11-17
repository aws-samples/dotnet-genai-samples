using Microsoft.Extensions.Options;
using Shop.Contracts;

namespace Shop.Api.Data;

/// <summary>Seeds the sample catalog on startup when the products store is empty.</summary>
public class CatalogSeeder(
    IServiceProvider services,
    IOptions<ShopOptions> options,
    ILogger<CatalogSeeder> logger) : IHostedService
{
    private static readonly ProductDto[] Catalog =
    [
        new("p-100", "Trail Runner Shoes", "Lightweight shoes with a grippy sole for mixed terrain.", "Footwear", 89.99m, "https://picsum.photos/seed/shoes/600/400", 40),
        new("p-101", "Everyday Sneakers", "Canvas sneakers that go with everything.", "Footwear", 54.50m, "https://picsum.photos/seed/sneakers/600/400", 75),
        new("p-200", "Insulated Water Bottle", "Keeps drinks cold for 24 hours, hot for 12.", "Outdoor", 24.00m, "https://picsum.photos/seed/bottle/600/400", 120),
        new("p-201", "Daypack 22L", "Water-resistant pack with a padded laptop sleeve.", "Outdoor", 69.00m, "https://picsum.photos/seed/daypack/600/400", 35),
        new("p-202", "Camp Hammock", "Packs down to the size of a grapefruit.", "Outdoor", 39.95m, "https://picsum.photos/seed/hammock/600/400", 50),
        new("p-300", "Noise-Cancelling Headphones", "30-hour battery and a comfortable over-ear fit.", "Electronics", 199.00m, "https://picsum.photos/seed/headphones/600/400", 25),
        new("p-301", "Smart Fitness Band", "Heart rate, sleep tracking, and 7-day battery.", "Electronics", 79.00m, "https://picsum.photos/seed/band/600/400", 60),
        new("p-302", "Portable Charger 20K", "Charges a phone four times over.", "Electronics", 45.00m, "https://picsum.photos/seed/charger/600/400", 90),
    ];

    public async Task StartAsync(CancellationToken ct)
    {
        if (!options.Value.SeedProducts) return;

        var products = services.GetRequiredService<IProductRepository>();
        try
        {
            if (await products.AnyAsync(ct)) return;
            foreach (var product in Catalog) await products.UpsertAsync(product, ct);
            logger.LogInformation("Seeded {Count} products", Catalog.Length);
        }
        catch (Exception ex)
        {
            // Don't block startup; the catalog pages will surface the error.
            logger.LogError(ex, "Catalog seeding failed. Check that the products table exists and credentials are valid.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
