<#
  Runs Shop.Api or Shop.Web under the ADOT .NET auto-instrumentation on Windows Server (EC2).

    .\scripts\run-windows.ps1 -App api
    .\scripts\run-windows.ps1 -App web
    .\scripts\run-windows.ps1 -App api -Mode console     # print spans/metrics, no agent needed

  Prerequisites on the instance:
    - .NET 10 SDK (or runtime, if you copy a published build)
    - Amazon CloudWatch agent, started with collector\cloudwatch-agent-config-windows.json:
        & "C:\Program Files\Amazon\AmazonCloudWatchAgent\amazon-cloudwatch-agent-ctl.ps1" `
          -a fetch-config -m ec2 -s -c file:collector\cloudwatch-agent-config-windows.json
    - An instance role with CloudWatchAgentServerPolicy

  The variables below are the same as scripts/run-local.sh; only the profiler path differs.
  The ADOT NuGet package puts the native profiler for every OS in the build output, so no
  separate install step is needed. (Alternative: the AWS.Otel.DotNet.Auto.psm1 installer from
  the ADOT GitHub releases, which installs machine-wide and can register IIS sites.)
#>
param(
  [Parameter(Mandatory)] [ValidateSet('api', 'web')] [string] $App,
  [ValidateSet('cloudwatch', 'console')] [string] $Mode = 'cloudwatch'
)
$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

if ($App -eq 'api') { $Project = 'Shop.Api'; $Port = 5100; $Sources = 'Shop.Api,Shop.Demo' }
else {
  $Project = 'Shop.Web'; $Port = 5200; $Sources = 'Shop.Web.Simulator'
  # MVC pages all share one route template; name the Application Signals operations by URL.
  $env:OTEL_AWS_HTTP_OPERATION_PATHS = '/Products/Details/{id},/Products,/Cart/Add,/Cart/Remove,/Cart,/Checkout,/Orders/Details/{id},/Orders,/Demo/Run,/Demo/Simulate,/Demo,/Home/Error,/'
}
$Service = "shop-$App"

dotnet build "src/$Project/$Project.csproj" -c Release -v q --nologo
if ($LASTEXITCODE) { exit $LASTEXITCODE }
$Out = (Resolve-Path "src/$Project/bin/Release/net10.0").Path

# --- Turn on the profiler (what instrument.cmd does) ----------------------------------------
$env:CORECLR_ENABLE_PROFILING = '1'
$env:CORECLR_PROFILER = '{918728DD-259F-4A6A-AC2B-B85E1B658318}'
$env:CORECLR_PROFILER_PATH = Join-Path $Out 'runtimes\win-x64\native\OpenTelemetry.AutoInstrumentation.Native.dll'
$env:DOTNET_STARTUP_HOOKS = Join-Path $Out 'OpenTelemetry.AutoInstrumentation.StartupHook.dll'
$env:ASPNETCORE_HOSTINGSTARTUPASSEMBLIES = 'OpenTelemetry.AutoInstrumentation.AspNetCoreBootstrapper'
$env:OTEL_DOTNET_AUTO_HOME = $Out
$env:OTEL_DOTNET_AUTO_REDIRECT_ENABLED = 'false'   # required for NuGet-based installs
$env:OTEL_DOTNET_AUTO_RULE_ENGINE_ENABLED = 'false'
$env:OTEL_DOTNET_AUTO_LOG_DIRECTORY = 'C:\shop-logs\otel-dotnet-auto'

# --- Who am I and what to instrument ---------------------------------------------------------
$env:OTEL_SERVICE_NAME = $Service
$env:OTEL_RESOURCE_ATTRIBUTES = "deployment.environment=ec2:dotnet-shop,aws.log.group.names=/dotnet-shop/$App"
$env:OTEL_DOTNET_AUTO_PLUGINS = 'AWS.Distro.OpenTelemetry.AutoInstrumentation.Plugin, AWS.Distro.OpenTelemetry.AutoInstrumentation:Shop.Telemetry.ShopInstrumentationPlugin, Shop.Telemetry'
$env:OTEL_DOTNET_AUTO_TRACES_ADDITIONAL_SOURCES = $Sources
$env:OTEL_DOTNET_AUTO_METRICS_ADDITIONAL_SOURCES = $Sources
$env:OTEL_PROPAGATORS = 'tracecontext,baggage'
$env:OTEL_TRACES_SAMPLER = 'parentbased_always_on'
$env:OTEL_LOGS_EXPORTER = 'none'                   # logs go through Serilog to the file

# Serilog log files where the CloudWatch agent tails them.
$env:Logging__File__Path = "C:\shop-logs\$Service.log"
$env:Logging__Journey__Path = 'C:\shop-logs\shopper-journey.log'

if ($Mode -eq 'console') {
  $env:OTEL_AWS_APPLICATION_SIGNALS_ENABLED = 'false'
  $env:OTEL_TRACES_EXPORTER = 'console'
  $env:OTEL_METRICS_EXPORTER = 'console'
  $env:RESOURCE_DETECTORS_ENABLED = 'false'
} else {
  $env:OTEL_AWS_APPLICATION_SIGNALS_ENABLED = 'true'
  $env:OTEL_EXPORTER_OTLP_PROTOCOL = 'http/protobuf'
  $env:OTEL_EXPORTER_OTLP_TRACES_ENDPOINT = 'http://127.0.0.1:4316/v1/traces'
  $env:OTEL_AWS_APPLICATION_SIGNALS_EXPORTER_ENDPOINT = 'http://127.0.0.1:4316/v1/metrics'
  $env:OTEL_METRICS_EXPORTER = 'otlp'              # business metrics -> DotnetShop namespace
  $env:OTEL_EXPORTER_OTLP_METRICS_ENDPOINT = 'http://127.0.0.1:4318/v1/metrics'
  $env:OTEL_EXPORTER_OTLP_METRICS_TEMPORALITY_PREFERENCE = 'delta'   # agent drops the first cumulative point of each series
}

# --- The API needs two AWS stacks (see README, Prerequisites), read with the instance role or your profile ---
if ($App -eq 'api') {
  function Get-StackOutput($Stack, $Key) {
    aws cloudformation describe-stacks --stack-name $Stack --query "Stacks[0].Outputs[?OutputKey=='$Key'].OutputValue" --output text 2>$null
  }
  $queue = Get-StackOutput 'dotnet-shop' 'OrderQueueUrl'
  if (-not $queue -or $queue -eq 'None') { throw 'Stack dotnet-shop not found. Deploy infra/shop-resources.yaml first (see README).' }
  $bucket = Get-StackOutput 'dotnet-shop-integrations' 'ReceiptsBucket'
  if (-not $bucket -or $bucket -eq 'None') { throw 'Stack dotnet-shop-integrations not found. Deploy infra/demo-integrations.yaml first (see README).' }
  $env:Shop__Aws__OrderQueueUrl = $queue
  $env:Shop__Integrations__ReceiptsBucket = $bucket
  $env:Shop__Integrations__PaymentSecretId = Get-StackOutput 'dotnet-shop-integrations' 'PaymentSecretArn'
}

Write-Host "==> $Service on http://localhost:$Port (ADOT auto-instrumentation, mode: $Mode)"
Set-Location "src/$Project"
dotnet (Join-Path $Out "$Project.dll") --urls "http://localhost:$Port"
