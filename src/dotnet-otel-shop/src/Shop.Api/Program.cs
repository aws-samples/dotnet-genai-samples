using Amazon.DynamoDBv2;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using Shop.Api;
using Shop.Api.Data;
using Shop.Api.Demo;
using Shop.Api.Diagnostics;
using Shop.Api.Endpoints;
using Shop.Api.Messaging;
using Shop.Api.Services;
using Shop.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.AddShopTelemetry("shop-api", ShopDiagnostics.SourceName, DemoTelemetry.SourceName);

builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection(ShopOptions.Section));
var shop = builder.Configuration.GetSection(ShopOptions.Section).Get<ShopOptions>() ?? new ShopOptions();

if (shop.UseAws)
{
    // Region and profile come from the "AWS" config section or the standard AWS environment/credential chain.
    builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddAWSService<IAmazonSQS>();

    builder.Services.AddSingleton<IProductRepository, DynamoDbProductRepository>();
    builder.Services.AddSingleton<ICartRepository, DynamoDbCartRepository>();
    builder.Services.AddSingleton<IOrderRepository, DynamoDbOrderRepository>();
    builder.Services.AddSingleton<IOrderEventPublisher, SqsOrderEventPublisher>();
    builder.Services.AddHostedService<SqsFulfillmentWorker>();
}
else
{
    builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
    builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
    builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
    builder.Services.AddSingleton<InMemoryOrderQueue>();
    builder.Services.AddSingleton<IOrderEventPublisher>(sp => sp.GetRequiredService<InMemoryOrderQueue>());
    builder.Services.AddHostedService<InMemoryFulfillmentWorker>();
}

builder.Services.AddSingleton<OrderFulfillmentProcessor>();
builder.Services.AddScoped<CheckoutService>();
builder.Services.AddHostedService<CatalogSeeder>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseShopRequestLogging();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapShopEndpoints();
app.MapDemoEndpoints();

app.Logger.LogInformation("Shop API starting with {Storage} storage",
    app.Services.GetRequiredService<IOptions<ShopOptions>>().Value.Storage);

app.Run();
