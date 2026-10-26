using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Shop.Telemetry;

public static class TelemetryExtensions
{
    /// <summary>
    /// Wires up OpenTelemetry traces, metrics, and logs for a Shop service.
    ///
    /// Config (section "Telemetry"):
    ///   Exporter: "otlp" (default), "console", or "both"
    ///   AwsResourceDetector: "none" (default), "ec2", "ecs", or "eks"
    /// Also enables the JSON log file (see FileLoggingExtensions, section "Logging:File").
    ///
    /// The OTLP exporter honors the standard OTEL_EXPORTER_OTLP_* environment
    /// variables (default endpoint http://localhost:4317).
    /// </summary>
    public static WebApplicationBuilder AddShopTelemetry(
        this WebApplicationBuilder builder,
        string serviceName,
        params string[] customSources)
    {
        var config = builder.Configuration.GetSection("Telemetry");
        var exporter = (config["Exporter"] ?? "otlp").ToLowerInvariant();
        var useOtlp = exporter is "otlp" or "both";
        var useConsole = exporter is "console" or "both";

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => ConfigureResource(resource, serviceName, builder.Environment, config))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(customSources)
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        // Keep health checks and static files out of traces.
                        o.Filter = ctx =>
                            !ctx.Request.Path.StartsWithSegments("/health") &&
                            !ctx.Request.Path.StartsWithSegments("/lib") &&
                            !ctx.Request.Path.StartsWithSegments("/css") &&
                            !ctx.Request.Path.StartsWithSegments("/js");
                        o.RecordException = true;

                        // MVC conventional routing names every span after the same template
                        // ("GET {controller=Home}/{action=Index}/{id?}"). Use "GET Products/Details"
                        // instead. This runs after the instrumentation sets the default name.
                        o.EnrichWithHttpResponse = (activity, response) =>
                        {
                            var values = response.HttpContext.GetRouteData().Values;
                            if (values["controller"] is string controller && values["action"] is string action)
                            {
                                var route = $"{controller}/{action}";
                                activity.DisplayName = $"{response.HttpContext.Request.Method} {route}";
                                activity.SetTag("http.route", route);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(o => o.RecordException = true)
                    .AddAWSInstrumentation();

                if (useConsole) tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(customSources)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddAWSInstrumentation();

                if (useConsole) metrics.AddConsoleExporter();
            })
            .WithLogging(logging =>
            {
                if (useConsole) logging.AddConsoleExporter();
            }, options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });

        // One call configures OTLP for all three signals.
        if (useOtlp) otel.UseOtlpExporter();

        // JSON log file for the CloudWatch agent (or any log shipper) to tail.
        builder.AddShopFileLogging(serviceName);

        return builder;
    }

    private static void ConfigureResource(
        ResourceBuilder resource,
        string serviceName,
        IHostEnvironment env,
        IConfiguration config)
    {
        resource
            .AddService(
                serviceName: serviceName,
                serviceVersion: typeof(TelemetryExtensions).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment.name"] = env.EnvironmentName,
            });

        // Detectors call instance metadata endpoints, so only enable the one
        // that matches where the app runs (they add startup latency elsewhere).
        switch ((config["AwsResourceDetector"] ?? "none").ToLowerInvariant())
        {
            case "ec2": resource.AddAWSEC2Detector(); break;
            case "ecs": resource.AddAWSECSDetector(); break;
            case "eks": resource.AddAWSEKSDetector(); break;
        }
    }
}
