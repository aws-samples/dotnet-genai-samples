#!/usr/bin/env bash
# Runs Shop.Api or Shop.Web with the ADOT .NET auto-instrumentation (macOS or Linux).
#
#   scripts/run-local.sh api            # send to the local CloudWatch agent (Application Signals)
#   scripts/run-local.sh web
#   scripts/run-local.sh api console    # no CloudWatch agent needed: print spans and metrics to the terminal
#   scripts/run-local.sh api both       # send to the agent AND print to the terminal
#
# The app has no telemetry code. Everything is the environment variables below.
# Requires the .NET 10 SDK (global.json pins it) and the AWS stacks described below.
set -euo pipefail
cd "$(dirname "$0")/.."

APP="${1:?usage: run-local.sh api|web [console|both]}"
MODE="${2:-cloudwatch}"
case "$MODE" in cloudwatch|console|both) ;; *) echo "unknown mode: $MODE (use console or both, or leave it out)" >&2; exit 1 ;; esac

case "$APP" in
  api) PROJECT=Shop.Api; PORT=5100; SOURCES=Shop.Api,Shop.Demo ;;
  web) PROJECT=Shop.Web; PORT=5200; SOURCES=Shop.Web.Simulator ;;
  *) echo "unknown app: $APP (use api or web)" >&2; exit 1 ;;
esac

dotnet build "src/$PROJECT/$PROJECT.csproj" -c Release -v q --nologo
OUT="$PWD/src/$PROJECT/bin/Release/net10.0"

# Which native profiler to load: osx-arm64, osx-x64, linux-arm64, or linux-x64.
case "$(uname -s)" in Darwin) OS=osx; EXT=dylib ;; *) OS=linux; EXT=so ;; esac
case "$(uname -m)" in arm64|aarch64) CPU=arm64 ;; *) CPU=x64 ;; esac

# ------------------------------------------------------------------------------------------
# 1. Load the auto-instrumentation. The .NET runtime reads these before the app starts.
# ------------------------------------------------------------------------------------------
export CORECLR_ENABLE_PROFILING=1
export CORECLR_PROFILER="{918728DD-259F-4A6A-AC2B-B85E1B658318}"
export CORECLR_PROFILER_PATH="$OUT/runtimes/$OS-$CPU/native/OpenTelemetry.AutoInstrumentation.Native.$EXT"
export DOTNET_STARTUP_HOOKS="$OUT/OpenTelemetry.AutoInstrumentation.StartupHook.dll"
export ASPNETCORE_HOSTINGSTARTUPASSEMBLIES=OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper
export OTEL_DOTNET_AUTO_HOME="$OUT"
export OTEL_DOTNET_AUTO_REDIRECT_ENABLED=false        # required when installed from NuGet
export OTEL_DOTNET_AUTO_LOG_DIRECTORY=/tmp/shop-logs/otel-dotnet-auto
mkdir -p "$OTEL_DOTNET_AUTO_LOG_DIRECTORY"

# ------------------------------------------------------------------------------------------
# 2. Name the service and pick up our own spans and metrics.
# ------------------------------------------------------------------------------------------
export OTEL_SERVICE_NAME="shop-$APP"
export OTEL_RESOURCE_ATTRIBUTES="deployment.environment=local,aws.log.group.names=/dotnet-shop/$APP"
export OTEL_DOTNET_AUTO_PLUGINS="AWS.Distro.OpenTelemetry.AutoInstrumentation.Plugin, AWS.Distro.OpenTelemetry.AutoInstrumentation:Shop.Telemetry.ShopInstrumentationPlugin, Shop.Telemetry"
export OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES="$SOURCES"   # our ActivitySource names
export OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES="$SOURCES"  # our Meter names
export OTEL_TRACES_SAMPLER=parentbased_always_on               # keep every trace for demos
export OTEL_LOGS_EXPORTER=none                                 # logs go through Serilog instead
export RESOURCE_DETECTORS_ENABLED=false   # off AWS the EC2 detector hangs startup; use true on EC2

# The storefront's MVC pages share one route template; name Application Signals operations by URL.
if [ "$APP" = web ]; then
  export OTEL_AWS_HTTP_OPERATION_PATHS="/Products/Details/{id},/Products,/Cart/Add,/Cart/Remove,/Cart,/Checkout,/Orders/Details/{id},/Orders,/Demo/Run,/Demo/Simulate,/Demo,/Home/Error,/"
fi

stack_out() { aws cloudformation describe-stacks --stack-name "$1" \
                --query "Stacks[0].Outputs[?OutputKey=='$2'].OutputValue" --output text 2>/dev/null; }

# The API needs two AWS stacks, both read with your default AWS credentials:
#   dotnet-shop               DynamoDB tables + SQS queue  (infra/shop-resources.yaml). Optional:
#                             without it the API stores data in memory. SHOP_STORAGE=InMemory forces that.
#   dotnet-shop-integrations  S3 receipts bucket + Secrets Manager payment key
#                             (infra/demo-integrations.yaml). Required.
# ADOT traces every call to these services with no code in the app.
if [ "$APP" = api ]; then
  stack_out() { aws cloudformation describe-stacks --stack-name "$1" \
                  --query "Stacks[0].Outputs[?OutputKey=='$2'].OutputValue" --output text 2>/dev/null; }

  bucket="$(stack_out dotnet-shop-integrations ReceiptsBucket)"
  if [ -z "$bucket" ] || [ "$bucket" = None ]; then
    echo "Stack dotnet-shop-integrations not found. Deploy infra/demo-integrations.yaml first (see README)." >&2
    exit 1
  fi
  export Shop__Integrations__ReceiptsBucket="$bucket"
  export Shop__Integrations__PaymentSecretId="$(stack_out dotnet-shop-integrations PaymentSecretArn)"
  echo "==> receipts: s3://$bucket, payment key: Secrets Manager"

  queue="$(stack_out dotnet-shop OrderQueueUrl)"
  if [ "${SHOP_STORAGE:-auto}" != InMemory ] && [ -n "$queue" ] && [ "$queue" != None ]; then
    export Shop__Storage=Aws
    export Shop__Aws__OrderQueueUrl="$queue"
    echo "==> storage: DynamoDB + SQS ($queue)"
  fi
fi

# ------------------------------------------------------------------------------------------
# 3. Choose where telemetry goes.
# ------------------------------------------------------------------------------------------
if [ "$MODE" = console ]; then
  export OTEL_TRACES_EXPORTER=console
  export OTEL_METRICS_EXPORTER=console
  export OTEL_AWS_APPLICATION_SIGNALS_ENABLED=false
else
  # Traces and Application Signals metrics -> CloudWatch agent (port 4316)
  export OTEL_AWS_APPLICATION_SIGNALS_ENABLED=true
  export OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
  export OTEL_EXPORTER_OTLP_TRACES_ENDPOINT=http://127.0.0.1:4316/v1/traces
  export OTEL_AWS_APPLICATION_SIGNALS_EXPORTER_ENDPOINT=http://127.0.0.1:4316/v1/metrics
  # Business metrics (shop.orders.placed, ...) -> CloudWatch agent OTLP receiver (port 4318)
  export OTEL_METRICS_EXPORTER=otlp
  export OTEL_EXPORTER_OTLP_METRICS_ENDPOINT=http://127.0.0.1:4318/v1/metrics
  # Send each interval's change (delta) instead of running totals. With running totals, the
  # agent uses the first value of every new series as a baseline and drops it, so the first
  # minute of each new metric (e.g. the first orders after startup) never reaches CloudWatch.
  export OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE=delta
  # "both": the same as above, plus a copy of every span and metric printed to the terminal.
  if [ "$MODE" = both ]; then
    export OTEL_TRACES_EXPORTER=otlp,console
    export OTEL_METRICS_EXPORTER=otlp,console
  fi
fi

# ------------------------------------------------------------------------------------------
# 4. Start the app. Plain "dotnet", no wrapper script.
# ------------------------------------------------------------------------------------------
echo "==> shop-$APP on http://localhost:$PORT (mode: $MODE)"
cd "src/$PROJECT"
exec dotnet "$OUT/$PROJECT.dll" --urls "http://localhost:$PORT"
