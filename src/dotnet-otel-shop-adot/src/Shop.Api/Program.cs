using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SQS;
using Microsoft.Extensions.Options;
using Shop.Api;
using Shop.Api.Data;
using Shop.Api.Demo;
using Shop.Api.Diagnostics;
using Shop.Api.Endpoints;
using Shop.Api.Integrations;
using Shop.Api.Messaging;
using Shop.Api.Services;
using Shop.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Traces and metrics: no code. The ADOT .NET auto-instrumentation attaches when the process
// starts (CLR profiler + startup hook, switched on by environment variables) and instruments
// ASP.NET Core, HttpClient, and the AWS SDK. The custom ActivitySource/Meter names
// (ShopDiagnostics.SourceName, DemoTelemetry.SourceName) are enabled by
// OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES / OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES.
//
// Logs: unchanged. Serilog writes JSON lines carrying the trace and span IDs.
builder.AddShopFileLogging("shop-api");

builder.Services.Configure<ShopOptions>(builder.Configuration.GetSection(ShopOptions.Section));
var shop = builder.Configuration.GetSection(ShopOptions.Section).Get<ShopOptions>() ?? new ShopOptions();

// Region and profile come from the "AWS" config section or the standard AWS environment/credential chain.
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

if (shop.UseAws)
{
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddAWSService<IAmazonSQS>();

    // No timing code: ADOT traces every DynamoDB and SQS call, and Application Signals turns those
    // spans into latency, error, and fault metrics per operation.
    builder.Services.AddSingleton<IProductRepository, DynamoDbProductRepository>();
    builder.Services.AddSingleton<ICartRepository, DynamoDbCartRepository>();
    builder.Services.AddSingleton<IOrderRepository, DynamoDbOrderRepository>();
    builder.Services.AddSingleton<IOrderEventPublisher, SqsOrderEventPublisher>();
    builder.Services.AddHostedService<SqsFulfillmentWorker>();
}
else
{
    // In-memory storage makes no network calls, so there is nothing for ADOT to trace here.
    builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
    builder.Services.AddSingleton<ICartRepository, InMemoryCartRepository>();
    builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
    builder.Services.AddSingleton<InMemoryOrderQueue>();
    builder.Services.AddSingleton<IOrderEventPublisher>(sp => sp.GetRequiredService<InMemoryOrderQueue>());
    builder.Services.AddHostedService<InMemoryFulfillmentWorker>();
}

// Checkout reads the payment key from Secrets Manager and writes receipts to S3. Both are required.
var integrations = shop.Integrations;
if (string.IsNullOrWhiteSpace(integrations.ReceiptsBucket) || string.IsNullOrWhiteSpace(integrations.PaymentSecretId))
    throw new InvalidOperationException(
        "Set Shop__Integrations__ReceiptsBucket and Shop__Integrations__PaymentSecretId " +
        "(deploy infra/demo-integrations.yaml to create the bucket and secret; scripts/run-local.sh sets them for you).");

builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddSingleton<SecretsManagerPaymentCredentialProvider>();
builder.Services.AddSingleton<S3OrderReceiptStore>();

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
