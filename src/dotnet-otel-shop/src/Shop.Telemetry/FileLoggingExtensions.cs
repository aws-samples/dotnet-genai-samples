using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Templates;

namespace Shop.Telemetry;

public static class FileLoggingExtensions
{
    /// <summary>
    /// Writes application logs as JSON lines to a file (one log entry per line), so the
    /// CloudWatch agent can ship them and CloudWatch Logs Insights can query fields directly.
    ///
    /// Config (section "Logging:File"):
    ///   Enabled:       true/false (default true)
    ///   Path:          file to write, e.g. /tmp/shop-logs/shop-api.log
    ///   MaxSizeMB:     roll to a new file after this size (default 10)
    ///   RetainedFiles: how many rolled files to keep (default 5)
    ///
    /// Config (section "Logging:Journey"): a second, plain-text file with only the
    /// "ShopperJourney" log category, one readable line per shopper action. Both apps
    /// write to the same file, so it reads as one story. For demos:
    ///   tail -f /tmp/shop-logs/shopper-journey.log
    ///   Enabled: true/false (default true)
    ///   Path:    default shopper-journey.log next to the main log file
    ///
    /// Console logging and the OpenTelemetry log exporter keep working: Serilog forwards
    /// every event to the other registered logging providers (writeToProviders: true).
    /// </summary>
    public static WebApplicationBuilder AddShopFileLogging(this WebApplicationBuilder builder, string serviceName)
    {
        var config = builder.Configuration.GetSection("Logging:File");
        if (!config.GetValue("Enabled", true)) return builder;

        var path = config["Path"] ?? Path.Combine(Path.GetTempPath(), "shop-logs", $"{serviceName}.log");
        var maxSizeMb = config.GetValue("MaxSizeMB", 10);
        var retainedFiles = config.GetValue("RetainedFiles", 5);
        var journey = builder.Configuration.GetSection("Logging:Journey");
        var journeyPath = journey["Path"] ?? Path.Combine(Path.GetDirectoryName(path) ?? ".", "shopper-journey.log");

        builder.Services.AddSerilog(logger => logger
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", serviceName)
                .Enrich.With<XRayTraceIdEnricher>()
                .WriteTo.File(
                    // One JSON object per line. Property names are plain (no "@") so they're
                    // easy to use in Logs Insights: fields traceId, level, message, ...
                    formatter: new ExpressionTemplate(
                        "{ {timestamp: UtcDateTime(@t), level: @l, message: @m, traceId: @tr, spanId: @sp, " +
                        "exception: @x, category: SourceContext, ..rest()} }\n"),
                    path: path,
                    fileSizeLimitBytes: maxSizeMb * 1024L * 1024L,
                    rollOnFileSizeLimit: true,
                    retainedFileCountLimit: retainedFiles,
                    shared: true)
                .WriteTo.Logger(sub =>
                {
                    if (!journey.GetValue("Enabled", true)) return;
                    sub.Filter.ByIncludingOnly(Matching.FromSource("ShopperJourney"))
                       .WriteTo.File(
                           path: journeyPath,
                           outputTemplate: "{Timestamp:HH:mm:ss}  {Message:lj}{NewLine}",
                           fileSizeLimitBytes: maxSizeMb * 1024L * 1024L,
                           rollOnFileSizeLimit: true,
                           retainedFileCountLimit: retainedFiles,
                           shared: true);
                }),
            writeToProviders: true);

        return builder;
    }

    /// <summary>Adds "xrayTraceId" so you can paste it straight into the X-Ray console.</summary>
    private sealed class XRayTraceIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            if (logEvent.TraceId is { } traceId)
                logEvent.AddPropertyIfAbsent(factory.CreateProperty("xrayTraceId", TraceIds.ToXRay(traceId)));
        }
    }
}
