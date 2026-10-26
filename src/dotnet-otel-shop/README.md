# Cloud Outfitters: .NET observability sample on AWS

A small e-commerce app for exploring OpenTelemetry with .NET on AWS:

- **Shop.Web**: ASP.NET Core MVC storefront (home, catalog, product detail, cart, checkout, orders)
- **Shop.Api**: ASP.NET Core REST API, using the AWS SDK for .NET (DynamoDB + SQS)
- **Shop.Telemetry**: shared OpenTelemetry setup (traces, metrics, logs)
- **Shop.Contracts**: DTOs shared by the web app and the API

```
Browser ──> Shop.Web (MVC) ──HTTP/REST──> Shop.Api ──AWS SDK──> DynamoDB (products, carts, orders)
                                              │
                                              └──AWS SDK──> SQS order-events ──> fulfillment worker
                     │                        │                                  (in Shop.Api)
                     └──────── OTLP ──────────┴──> OTel Collector (ADOT) ──> X-Ray / CloudWatch
```

One checkout produces a single trace that spans both services and the async worker:

```
POST Checkout/Index                       shop-web
└─ POST (HttpClient)                      shop-web
   └─ POST /api/orders                    shop-api
      └─ Checkout
         ├─ DynamoDB.GetItem (cart)
         ├─ ValidateStock → DynamoDB.GetItem
         ├─ ProcessPayment                (simulated latency, ~10% declined)
         ├─ DynamoDB.PutItem (order)
         ├─ DynamoDB.DeleteItem (cart)
         └─ SQS.SendMessage               (traceparent injected into message attributes)
            └─ order-events process       (consumer, continues the same trace)
               └─ DynamoDB.UpdateItem     (status → Fulfilled)
```

## Run locally (no AWS needed)

Requires the .NET 8 SDK. By default the API uses in-memory storage and an in-process queue.

```bash
# Terminal 1: API on http://localhost:5100
Telemetry__Exporter=console dotnet run --project src/Shop.Api

# Terminal 2: storefront on http://localhost:5200
Telemetry__Exporter=console dotnet run --project src/Shop.Web
```

`Telemetry__Exporter=console` prints spans, metrics, and logs to the terminal. To see traces in a UI,
leave the exporter at the default (`otlp`) and run any OTLP backend on `localhost:4317`, such as
Jaeger, the .NET Aspire dashboard, or the collector in `collector/`.

## Run against AWS

1. **Create the resources** (DynamoDB tables, SQS queue with DLQ, and an IAM policy for the API):

   ```bash
   aws cloudformation deploy --stack-name dotnet-shop \
     --template-file infra/shop-resources.yaml --capabilities CAPABILITY_IAM
   aws cloudformation describe-stacks --stack-name dotnet-shop \
     --query "Stacks[0].Outputs" --output table
   ```

2. **Run a collector** with `collector/otel-collector-config.yaml`. Use the
   [ADOT collector](https://aws-otel.github.io/docs/getting-started/collector) (EC2, ECS sidecar, EKS add-on),
   or `otelcol-contrib` locally. The collector needs `AWS_REGION` and permissions for
   `xray:PutTraceSegments`, `xray:PutTelemetryRecords`, `logs:CreateLogGroup`, `logs:CreateLogStream`,
   and `logs:PutLogEvents`. The AWS-managed `AWSXrayWriteOnlyAccess` and `CloudWatchAgentServerPolicy`
   policies cover these.

3. **Start the API in AWS mode** (credentials come from the default AWS chain: env vars, profile, or role):

   ```bash
   Shop__Storage=Aws \
   Shop__Aws__OrderQueueUrl=<OrderQueueUrl output> \
   AWS__Region=us-east-1 \
   dotnet run --project src/Shop.Api
   ```

   On startup the API seeds the products table if it's empty.

4. **Start Shop.Web** as above, then shop. Traces appear in the X-Ray / CloudWatch trace map, metrics
   under the `DotnetShop` namespace, and logs in `/dotnet-shop/app`.

To clean up: `aws cloudformation delete-stack --stack-name dotnet-shop`. This deletes the tables and their data.

## What's instrumented

| Signal | Source | Examples |
|---|---|---|
| Traces | ASP.NET Core | `GET Products/Details`, `POST /api/orders` |
| Traces | HttpClient | Web → API calls, with W3C `traceparent` propagation |
| Traces | AWS SDK (`OpenTelemetry.Instrumentation.AWS`) | `DynamoDB.GetItem`, `DynamoDB.Query`, `SQS.SendMessage` |
| Traces | Custom (`Shop.Api` ActivitySource) | `Checkout`, `ValidateStock`, `ProcessPayment`, `order-events process` |
| Metrics | ASP.NET Core, HttpClient, .NET runtime | `http.server.request.duration`, GC, thread pool |
| Metrics | AWS SDK | AWS SDK client metrics |
| Metrics | Custom (`Shop.Api` Meter) | `shop.orders.placed`, `shop.order.value`, `shop.checkout.failures{reason}`, `shop.cart.items_added{product.category}`, `shop.orders.fulfilled` |
| Logs | `ILogger` via OpenTelemetry, plus a JSON file (Serilog) | Every log carries `traceId`/`spanId` for correlation |
| All | Telemetry demo (`Shop.Demo` source/meter) | `DemoWorkflow` spans, `demo.button.clicks`, `demo.work.duration`, `demo.jobs.active`, `demo.queue.depth` |

The storefront error page shows the trace ID, so you can paste it into X-Ray to find the failing request.

## Telemetry demo

Open **Telemetry Demo** in the storefront nav (http://localhost:5200/Demo). Each button creates one kind of
telemetry and shows the trace ID and X-Ray trace ID to look up in AWS.

| Button | What it creates | Code |
|---|---|---|
| Write logs | Structured logs, log levels, a scope, a source-generated log | `DemoEndpoints.Logs` |
| Create a trace | A parent span with 3 child spans, attributes, and an event | `DemoEndpoints.Traces` |
| Record metrics | Counter, histogram, up/down counter, observable gauge | `DemoEndpoints.Metrics` |
| Cause an error | Failed span with exception, error log with stack trace, HTTP 500 | `DemoEndpoints.Error` |

Files to read, in this order:

1. `src/Shop.Api/Demo/DemoTelemetry.cs`: declares the `ActivitySource` (traces) and `Meter` (metrics), with one example of each instrument type
2. `src/Shop.Api/Demo/DemoEndpoints.cs`: the examples themselves, commented step by step
3. `src/Shop.Telemetry/TelemetryExtensions.cs`: how OpenTelemetry is wired up (instrumentation, exporters, resource)
4. `src/Shop.Telemetry/FileLoggingExtensions.cs`: the JSON log file

## Shopper journey log (for demos)

A plain-English log of what shoppers do, written by both apps to one file:

```bash
tail -f /tmp/shop-logs/shopper-journey.log
```

```
15:18:41  Shopper e704b1 opened the home page
15:18:42  Shopper e704b1 clicked on Trail Runner Shoes ($89.99)
15:18:42  Shopper e704b1 added 2 x Trail Runner Shoes to the cart. Cart: 2 items, $179.98
15:18:42  Shopper e704b1 started checkout for $179.98
15:18:42  Shopper e704b1 paid $179.98. Order ord-32958465835a confirmed
15:18:43  Warehouse shipped order ord-32958465835a for shopper e704b1 (2 items, $179.98)
```

Shoppers are identified by the first 6 characters of their anonymous session id. To follow one shopper:
`grep e704b1 /tmp/shop-logs/shopper-journey.log`. The same lines are in the JSON logs with
`category = "ShopperJourney"`, and the CloudWatch agent ships the file to the `/dotnet-shop/journey` log group.
The messages are defined in `src/Shop.Web/Services/ShopperJourney.cs`.

## Log files

Both apps write JSON logs, one object per line, in addition to the console:

- `/tmp/shop-logs/shop-api.log`
- `/tmp/shop-logs/shop-web.log`

Each line includes `timestamp`, `level`, `message`, `traceId`, `spanId`, `xrayTraceId`, `service`, and every
structured log property (e.g. `OrderId`). Files roll at 10 MB and the last 5 are kept. Change this under
`Logging:File` in `appsettings.json`, or override the path with `Logging__File__Path=...`.

The CloudWatch agent config (`collector/cloudwatch-agent-config.json`) tails these files.

### What gets logged

- **One line per HTTP request:** `HTTP GET /Products/Details/p-100 responded 200 in 12 ms`, with
  `RequestMethod`, `RequestPath`, `StatusCode`, `Elapsed`, and `SessionId`. 4xx is logged as Warning and 5xx as Error.
- **Business events**, each with a stable `EventId.Name`, defined in `src/Shop.Api/Diagnostics/ShopLog.cs` and
  `src/Shop.Web/Services/WebLog.cs`:

| Event | Level | Key fields |
|---|---|---|
| `CatalogListed`, `ProductViewed`, `ProductNotFound` | Info / Warning | `ProductId`, `Price`, `Stock` |
| `CartItemAdded`, `CartItemRemoved`, `CartQuantityRejected` | Info / Warning | `SessionId`, `ProductId`, `Quantity`, `CartTotal` |
| `CheckoutStarted`, `OrderPlaced`, `CheckoutFailed` | Info / Warning | `OrderId`, `OrderTotal`, `CheckoutMs`, `Reason` |
| `PaymentAuthorized`, `PaymentDeclined` | Info / Warning | `Amount`, `LatencyMs` |
| `OrderEventReceived`, `OrderFulfilled`, `FulfillmentFailed` | Info / Error | `QueueLatencyMs`, `FulfillmentMs` |
| `CheckoutFormInvalid`, `CheckoutRejected`, `OrderConfirmed` (web) | Warning / Info | `InvalidFields` (names only), `Reason` |
| `SimulatedShopperFinished`, `SimulationFinished` (web) | Info | `Outcome`, `Orders`, `Declined` |

Names, emails, and addresses are never logged. Shoppers are identified only by an anonymous `SessionId`.

To fill the logs with realistic data, open **Telemetry Demo** and click **Run simulation**. 25 shoppers take
about 3 seconds.

### Logs Insights queries

Select the log groups `/dotnet-shop/api` and `/dotnet-shop/web`, then:

```
# Which events happened, and how often
stats count(*) as events by EventId.Name | sort events desc

# Everything for one request or trace (paste a traceId)
fields @timestamp, service, level, message | filter traceId = "<trace id>" | sort @timestamp

# Orders and revenue per 5 minutes
filter EventId.Name = "OrderPlaced" | stats count(*) as orders, sum(OrderTotal) as revenue by bin(5m)

# Payment latency and decline rate
filter EventId.Name in ["PaymentAuthorized", "PaymentDeclined"]
| stats count(*) as attempts, avg(LatencyMs) as avgMs, pct(LatencyMs, 95) as p95Ms by EventId.Name

# Why checkouts fail
filter EventId.Name = "CheckoutFailed" | stats count(*) by Reason

# Slowest requests
filter ispresent(Elapsed) | sort Elapsed desc | limit 20 | fields @timestamp, service, RequestMethod, RequestPath, StatusCode, Elapsed

# Errors and warnings
fields @timestamp, service, level, message, exception | filter level in ["Error", "Warning"] | sort @timestamp desc
```

## Configuration

Set these in `appsettings.json` or as environment variables (`__` separates sections).

| Setting | Default | Notes |
|---|---|---|
| `Telemetry:Exporter` | `otlp` | `otlp`, `console`, or `both` |
| `Telemetry:AwsResourceDetector` | `none` | `ec2`, `ecs`, or `eks` adds cloud/host resource attributes. Leave it `none` off AWS, because detectors add startup latency. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `http://localhost:4317` | Standard OTel env var |
| `Shop:Storage` (API) | `InMemory` | `Aws` uses DynamoDB + SQS |
| `Shop:Aws:*` (API) | `shop-*` names | Table names, GSI name, `OrderQueueUrl` |
| `Shop:Chaos:PaymentFailureRate` (API) | `0.1` | Fraction of payments declined |
| `Shop:Chaos:PaymentMin/MaxLatencyMs` (API) | `50` / `600` | Simulated payment latency |
| `AWS:Region` / `AWS:Profile` (API) | `us-east-1` | Standard AWS SDK settings |
| `ShopApi:BaseUrl` (Web) | `http://localhost:5100` | Where the storefront finds the API |
| `Logging:File:Enabled` / `Path` | `true` / `/tmp/shop-logs/shop-{api,web}.log` | JSON log file |
| `Logging:File:MaxSizeMB` / `RetainedFiles` | `10` / `5` | Log file rotation |

## Notes

- The product catalog uses a DynamoDB `Scan`, which is fine for eight items. For real workloads, add a GSI on `Category`.
- X-Ray accepts W3C trace IDs, so no X-Ray ID generator or propagator is needed. If you put an ALB in front, its
  `X-Amzn-Trace-Id` header is a separate trace. Add `OpenTelemetry.Extensions.AWS` and the `AWSXRayPropagator` if you
  want to join them.
- For CloudWatch Application Signals, the ADOT .NET auto-instrumentation is an alternative to this manual SDK setup.
