using Amazon.DynamoDBv2;
using Amazon.S3;
using Amazon.SecretsManager;
using Amazon.SQS;
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

// The sample needs the AWS resources from infra/shop-resources.yaml (DynamoDB tables, SQS queue) and
// infra/demo-integrations.yaml (S3 receipts bucket, Secrets Manager payment key).
if (string.IsNullOrWhiteSpace(shop.Aws.OrderQueueUrl) ||
    string.IsNullOrWhiteSpace(shop.Integrations.ReceiptsBucket) ||
    string.IsNullOrWhiteSpace(shop.Integrations.PaymentSecretId))
    throw new InvalidOperationException(
        "Set Shop__Aws__OrderQueueUrl, Shop__Integrations__ReceiptsBucket and Shop__Integrations__PaymentSecretId " +
        "(scripts/run-local.sh reads them from the dotnet-shop and dotnet-shop-integrations stacks).");

// Region and profile come from the "AWS" config section or the standard AWS environment/credential chain.
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddAWSService<IAmazonSecretsManager>();
builder.Services.AddAWSService<IAmazonS3>();

// No timing code anywhere: ADOT traces every DynamoDB, SQS, S3 and Secrets Manager call, and
// Application Signals turns those spans into latency, error, and fault metrics per operation.
builder.Services.AddSingleton<IProductRepository, DynamoDbProductRepository>();
builder.Services.AddSingleton<ICartRepository, DynamoDbCartRepository>();
builder.Services.AddSingleton<IOrderRepository, DynamoDbOrderRepository>();
builder.Services.AddSingleton<IOrderEventPublisher, SqsOrderEventPublisher>();
builder.Services.AddHostedService<SqsFulfillmentWorker>();
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

app.Logger.LogInformation("Shop API starting with DynamoDB and SQS storage");

app.Run();
