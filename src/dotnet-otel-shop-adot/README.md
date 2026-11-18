# Cloud Outfitters: .NET observability with ADOT auto-instrumentation and Application Signals

This is a copy of [`dotnet-otel-shop`](../dotnet-otel-shop) with the manual OpenTelemetry SDK setup removed. Traces and
metrics now come from the **AWS Distro for OpenTelemetry (ADOT) .NET auto-instrumentation**, and
**CloudWatch Application Signals** turns them into service health, operations, a service map, and SLOs.
Logs are unchanged: Serilog writes JSON lines with trace IDs.

Architecture diagrams (open in a browser):

- [`docs/architecture-adot-appsignals.html`](docs/architecture-adot-appsignals.html): how ADOT and Application Signals fit together
- [`docs/demo-architecture.html`](docs/demo-architecture.html): the sample on Amazon ECS with AWS Fargate ([PNG](docs/demo-architecture.png))

## What changed compared with dotnet-otel-shop

| | dotnet-otel-shop (manual SDK) | dotnet-otel-shop-adot (this copy) |
|---|---|---|
| Telemetry packages | 8 OpenTelemetry packages in Shop.Telemetry | One package per app: `AWS.Distro.OpenTelemetry.AutoInstrumentation` |
| Startup code | `builder.AddShopTelemetry(...)` (~140 lines, TelemetryExtensions.cs) | None. `TelemetryExtensions.cs` is deleted |
| How it's switched on | Code runs at startup | Environment variables load a CLR profiler and startup hook before `Main` |
| Library spans (ASP.NET Core, HttpClient, AWS SDK) | Registered in code | Automatic |
| Custom spans/metrics (`Checkout`, `shop.orders.placed`, ...) | `AddSource(...)` / `AddMeter(...)` | Same code, unchanged. Names listed in `OTEL_DOTNET_AUTO_*_ADDITIONAL_SOURCES` |
| Agent config | OTLP receivers for traces + metrics | `application_signals` for traces + RED metrics; OTLP kept for business metrics |
| In CloudWatch | X-Ray traces, `DotnetShop` metrics | Plus Application Signals: per-service and per-operation latency, errors, faults, service map, SLOs |
| Logs | Serilog JSON with traceId/spanId | Same |

Files that hold the change:

- `src/Shop.Api/Shop.Api.csproj`, `src/Shop.Web/Shop.Web.csproj`: the ADOT package (plus two pinned/workaround references, explained below)
- `src/Shop.Api/Program.cs`, `src/Shop.Web/Program.cs`: `AddShopTelemetry` replaced by `AddShopFileLogging` (logs only)
- `src/Shop.Telemetry/ShopInstrumentationPlugin.cs`: optional plugin, only filters `/health` and static files out of traces
- `scripts/run-local.sh`, `scripts/run-windows.ps1`: every environment variable, commented
- `collector/cloudwatch-agent-config*.json`, `infra/ecs-fargate.yaml`: Application Signals on the agent and the task

## How auto-instrumentation works

```
dotnet Shop.Api.dll
  │
  ├─ CORECLR_ENABLE_PROFILING=1 + CORECLR_PROFILER_PATH   → the .NET runtime loads the native CLR profiler
  ├─ DOTNET_STARTUP_HOOKS=...StartupHook.dll               → runs before Main: builds TracerProvider/MeterProvider
  ├─ ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=...Bootstrapper   → hooks into ASP.NET Core startup
  └─ OTEL_DOTNET_AUTO_PLUGINS=AWS...Plugin                 → ADOT adds AWS resource, X-Ray IDs,
                                                             Application Signals metrics and attributes
```

The app code never references OpenTelemetry. `ShopDiagnostics.cs`, `DemoTelemetry.cs`, and
`CheckoutService.cs` still use plain `System.Diagnostics` `ActivitySource` and `Meter`, so they work
the same under the manual SDK or auto-instrumentation. That's the main teaching point: **instrument
with .NET's built-in APIs, and choose how to export at deploy time.**

The ADOT NuGet package (rather than the machine-wide installer) puts the profiler for every OS into the
build output, plus `instrument.sh` / `instrument.cmd`, which set the profiler variables. It also works on
macOS for local runs.

## Prerequisites

- The .NET 10 SDK (`global.json` pins it), the AWS CLI, and AWS credentials for an account you can deploy to.
- Two CloudFormation stacks in your account and region. The API calls these services, and ADOT traces
  every call with no code in the app:

| Stack | Template | Creates | Required |
|---|---|---|---|
| `dotnet-shop-integrations` | `infra/demo-integrations.yaml` | S3 receipts bucket, Secrets Manager payment key, IAM policy | Yes |
| `dotnet-shop` | `infra/shop-resources.yaml` | DynamoDB tables, SQS queue with a dead-letter queue, IAM policy | No. Without it the API stores data in memory |

```bash
aws cloudformation deploy --stack-name dotnet-shop-integrations \
  --template-file infra/demo-integrations.yaml --capabilities CAPABILITY_IAM
aws cloudformation deploy --stack-name dotnet-shop \
  --template-file infra/shop-resources.yaml --capabilities CAPABILITY_IAM
```

To clean up, empty the receipts bucket (`aws s3 rm s3://<ReceiptsBucket output> --recursive`) and delete
both stacks. Deleting `dotnet-shop` removes the tables and their data.

## Run locally

```bash
scripts/run-local.sh api      # http://localhost:5100
scripts/run-local.sh web      # http://localhost:5200

# Print spans and metrics to the terminal instead of sending them to the CloudWatch agent:
scripts/run-local.sh api console
scripts/run-local.sh web console
```

`run-local.sh api` reads the stack outputs with your default AWS credentials and sets
`Shop__Integrations__ReceiptsBucket`, `Shop__Integrations__PaymentSecretId`, and, when the `dotnet-shop`
stack exists, `Shop__Storage=Aws` and `Shop__Aws__OrderQueueUrl`. `SHOP_STORAGE=InMemory` forces in-memory
storage. If the integrations stack is missing, the script stops and says so; the API itself also refuses to
start without the bucket and secret settings.

Checkout reads the payment gateway key from Secrets Manager and writes a JSON receipt (no customer name,
email, or address) to S3. There is no tracing code for either: the ADOT AWS SDK instrumentation creates the
spans, and Application Signals lists DynamoDB, SQS, S3, and Secrets Manager as shop-api dependencies. Code:
`src/Shop.Api/Integrations/AwsIntegrations.cs`. The secret is read on every checkout so each trace shows it;
a production service would cache it.

To start the CloudWatch agent on a Mac, use `collector/cloudwatch-agent-config.json` and point the agent at
your AWS profile with `collector/common-config.toml` (the agent runs as root).

## Run on Amazon EC2 (Windows Server)

1. Attach an instance role with `CloudWatchAgentServerPolicy`, and enable Application Signals in the account once.
2. Install the CloudWatch agent and start it with `collector/cloudwatch-agent-config-windows.json`.
3. Create the stacks from Prerequisites, and attach their managed policies (`ShopApiPolicy`, `ShopApiIntegrationsPolicy`) to the instance role.
4. `.\scripts\run-windows.ps1 -App api` and `.\scripts\run-windows.ps1 -App web`.

Serilog writes to `C:\shop-logs\*.log`; the agent tails them into `/dotnet-shop/{api,web,journey}`.

## Run on Amazon ECS on AWS Fargate

`infra/deploy-ecs.sh` needs the stacks from Prerequisites (`dotnet-shop-integrations` is required, `dotnet-shop` is optional). The profiler is already in each image (`/app`), so the task
definition only adds environment variables; no init container is needed. The CloudWatch agent sidecar
config now enables `application_signals`.

## Things we found (and how they are handled)

| Issue | Symptom | Handling |
|---|---|---|
| The ADOT 1.15.0 NuGet package does not ship `OpenTelemetry.Sampler.AWS.dll` | `FileNotFoundException` in the auto-instrumentation log; no metrics exported | DLL taken from the official release zip (SHA-256 checked) into `lib/adot-1.15.0`, referenced from both apps. Remove when fixed upstream |
| ADOT's package lets NuGet resolve the oldest upstream auto-instrumentation (1.7.0) | Version mismatch with ADOT's OpenTelemetry 1.16 | `OpenTelemetry.AutoInstrumentation` pinned to 1.16.0 |
| ADOT always runs the EC2/ECS/EKS resource detectors | Off AWS, the app hangs at startup waiting for instance metadata | `run-local.sh` sets `RESOURCE_DETECTORS_ENABLED=false` unless instance metadata answers |
| Profiler's default log folder `/var/log/opentelemetry` | Process aborts on macOS | `OTEL_DOTNET_AUTO_LOG_DIRECTORY` set |
| `OTEL_PROPAGATORS=...,xray` | "Propagator 'xray' is not supported" | Not needed: with Application Signals on, ADOT installs W3C, B3, and X-Ray propagators itself |
| MVC conventional routing | Every storefront page is one operation, `GET {controller=Home}/{action=Index}/{id?}` | `OTEL_AWS_HTTP_OPERATION_PATHS` maps URLs to operations, for example `GET /Products/Details/{id}` |
| Cumulative metrics through the CloudWatch agent | The first export of every new metric series is used as a baseline and dropped, so early orders are missing from CloudWatch | `OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE=delta` in `run-local.sh`: the app sends per-interval changes, which the agent passes through as-is |
| AWS SDK for .NET v4 | The CloudWatch docs list SDK v3 for full Application Signals support | Kept v4. Verified on v4: DynamoDB, SQS, S3 and Secrets Manager calls all appear as shop-api dependencies with their table, queue, bucket and secret. The SQS consumer span shows as an `aws_sqs` node with `UnknownRemoteOperation` |
| Vulnerable transitive package: `OpenTelemetry.Resources.Host` 1.15.1-beta.1 | `NU1903` build warning and a GitHub Dependabot alert for [GHSA-v8pv-4842-x354](https://github.com/advisories/GHSA-v8pv-4842-x354) (high severity, fixed in 1.16.0-beta.2). The advisory describes arbitrary code execution through local PATH hijacking, on macOS only. It comes in through `OpenTelemetry.AutoInstrumentation` 1.16.0, which ADOT 1.15.0 requires | Not overridden: ADOT 1.15.0 is the latest release and is built and tested against this version. On macOS, run the sample only on a machine you trust. To clear it, move to an ADOT release built on a newer upstream auto-instrumentation and remove the `OpenTelemetry.AutoInstrumentation` pin from both `.csproj` files |

---

# Reference

A small e-commerce app for exploring OpenTelemetry with .NET on AWS:

- **Shop.Web**: ASP.NET Core MVC storefront (home, catalog, product detail, cart, checkout, orders)
- **Shop.Api**: ASP.NET Core REST API, using the AWS SDK for .NET (DynamoDB, SQS, S3, Secrets Manager)
- **Shop.Telemetry**: Serilog JSON logging and the optional auto-instrumentation plugin (no OpenTelemetry SDK setup in this copy)
- **Shop.Contracts**: DTOs shared by the web app and the API

```
Browser ──> Shop.Web (MVC) ──HTTP──> Shop.Api ──AWS SDK──> DynamoDB (products, carts, orders)
                                        ├──AWS SDK──> Secrets Manager (payment key)
                                        ├──AWS SDK──> S3 (receipts)
                                        └──AWS SDK──> SQS order-events ──> fulfillment worker (in Shop.Api)

Both apps (ADOT auto-instrumentation) ──OTLP──> CloudWatch agent ──> X-Ray / Application Signals / CloudWatch
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
         │  └─ SecretsManager.GetSecretValue
         ├─ DynamoDB.PutItem (order)
         ├─ DynamoDB.DeleteItem (cart)
         ├─ SQS.SendMessage               (traceparent injected into message attributes)
         │  └─ order-events process       (consumer, continues the same trace)
         │     └─ DynamoDB.UpdateItem     (status → Fulfilled)
         └─ S3.PutObject (receipt)
```

## Deploy to ECS on Fargate

```bash
infra/deploy-ecs.sh    # prints the storefront URL when done
```

No Docker needed: the .NET SDK builds ARM64 images and pushes them to ECR (`infra/ecr.yaml`), then
`infra/ecs-fargate.yaml` creates a VPC, an ECS cluster, and the two services.

- **Access:** the storefront ALB accepts HTTP only from `ALLOWED_CIDR` (default: your current public IP /32).
  shop-api has no public endpoint; shop-web reaches it as `http://shop-api:8080` through Service Connect.
  If your IP changes, re-run the script (or set `ALLOWED_CIDR`).
- **Traces and metrics:** each task runs a CloudWatch agent sidecar on `127.0.0.1:4317`, the same OTLP
  setup as running locally. Traces go to X-Ray, metrics to the `DotnetShop` namespace.
- **Logs:** files inside a container are lost when it stops, so the apps write the same JSON lines to stdout
  (`Logging__Console__Json=true`, `Logging__File__Enabled=false`). The awslogs driver ships them to
  `/dotnet-shop/ecs/web` and `/dotnet-shop/ecs/api`. The Logs Insights queries below work unchanged; for the
  shopper journey, filter `category = "ShopperJourney"`.
- **Cost:** the NAT gateway, ALB, and two Fargate tasks bill while the stack exists.

To clean up: `aws cloudformation delete-stack --stack-name dotnet-shop-ecs`, then empty the two ECR
repositories and delete `dotnet-shop-ecr`.

## What's instrumented

| Signal | Source | Examples |
|---|---|---|
| Traces | ASP.NET Core | `GET Products/Details`, `POST /api/orders` |
| Traces | HttpClient | Web → API calls, with W3C `traceparent` propagation |
| Traces | AWS SDK (`OpenTelemetry.Instrumentation.AWS`) | `DynamoDB.GetItem`, `DynamoDB.Query`, `SQS.SendMessage`, `S3.PutObject`, `SecretsManager.GetSecretValue` |
| Traces | Custom (`Shop.Api` ActivitySource) | `Checkout`, `ValidateStock`, `ProcessPayment`, `order-events process` |
| Metrics | ASP.NET Core, HttpClient, .NET runtime | `http.server.request.duration`, GC, thread pool |
| Metrics | AWS SDK | AWS SDK client metrics |
| Metrics | Custom (`Shop.Api` Meter) | `shop.orders.placed`, `shop.order.value`, `shop.checkout.failures{reason}`, `shop.cart.items_added{product.category}`, `shop.orders.fulfilled` |
| Logs | `ILogger` via Serilog, as JSON lines to a file (or stdout in containers) | Every log carries `traceId`/`spanId` for correlation |
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
3. `scripts/run-local.sh`: how OpenTelemetry is wired up in this copy (environment variables, no code)
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
| `Shop:Storage` (API) | `InMemory` | `Aws` uses DynamoDB + SQS (`run-local.sh` sets it when the `dotnet-shop` stack exists) |
| `Shop:Integrations:ReceiptsBucket` / `PaymentSecretId` (API) | none, required | S3 bucket and Secrets Manager secret from `dotnet-shop-integrations` |
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
