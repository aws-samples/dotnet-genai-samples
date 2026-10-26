using Shop.Telemetry;
using Shop.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddShopTelemetry("shop-web", ShopperSimulator.SourceName);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<ShopApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ShopApi:BaseUrl"] ?? "http://localhost:5100");
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddScoped<ShopperSimulator>();

var app = builder.Build();

// Always use the friendly error page; it shows the trace id for support lookups.
app.UseExceptionHandler("/Home/Error");
app.UseStaticFiles();
app.UseShopSession();
app.UseShopRequestLogging();
app.UseRouting();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
