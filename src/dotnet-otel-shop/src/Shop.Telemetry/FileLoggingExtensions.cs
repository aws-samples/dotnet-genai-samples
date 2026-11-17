using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Templates;

namespace Shop.Telemetry;

public static class FileLoggingExtensions
{
    // One JSON object per line. Property names are plain (no "@") so they're
    // easy to use in Logs Insights: fields traceId, level, message, ...
    private const string JsonLineTemplate =
        "{ {timestamp: UtcDateTime(@t), level: @l, message: @m, traceId: @tr, spanId: @sp, " +
        "exception: @x, category: SourceContext, ..rest()} }\n";

    /// <summary>
    /// Writes application logs as JSON lines (one log entry per line), so CloudWatch Logs
    /// Insights can query fields directly. Two destinations, usable together:
    ///
    /// Config (section "Logging:File"): a file for the CloudWatch agent to tail (EC2, laptop).
    ///   Enabled:       true/false (default true)
    ///   Path:          file to write, e.g. /tmp/shop-logs/shop-api.log
    ///   MaxSizeMB:     roll to a new file after this size (default 10)
    ///   RetainedFiles: how many rolled files to keep (default 5)
    ///
    /// Config (section "Logging:Console"): stdout, for containers (ECS awslogs driver, FireLens,
    /// Fluent Bit on EKS). Files inside a container are lost when it stops.
    ///   Json: true/false (default false). Replaces the plain-text console logger, so each
    ///         entry is written once, as JSON.
    ///
    /// Config (section "Logging:Journey"): a second, plain-text file with only the
    /// "ShopperJourney" log category, one readable line per shopper action. Both apps
    /// write to the same file, so it reads as one story. Only written when the log file is
    /// enabled. For demos:
    ///   tail -f /tmp/shop-logs/shopper-journey.log
    ///   Enabled: true/false (default true)
    ///   Path:    default shopper-journey.log next to the main log file
    ///
    /// Other logging providers keep working: Serilog forwards every event to them
    /// (writeToProviders: true).
    /// </summary>
    public static WebApplicationBuilder AddShopFileLogging(this WebApplicationBuilder builder, string serviceName)
    {
        var config = builder.Configuration.GetSection("Logging:File");
        var fileEnabled = config.GetValue("Enabled", true);
        var jsonConsole = builder.Configuration.GetValue("Logging:Console:Json", false);
        if (!fileEnabled && !jsonConsole) return builder;

        var path = config["Path"] ?? Path.Combine(Path.GetTempPath(), "shop-logs", $"{serviceName}.log");
        var maxSizeMb = config.GetValue("MaxSizeMB", 10);
        var retainedFiles = config.GetValue("RetainedFiles", 5);
        var journey = builder.Configuration.GetSection("Logging:Journey");
        var journeyPath = journey["Path"] ?? Path.Combine(Path.GetDirectoryName(path) ?? ".", "shopper-journey.log");

        // Serilog writes the JSON to stdout, so drop the default plain-text console logger
        // instead of printing every entry twice in two formats.
        if (jsonConsole)
            builder.Logging.Services.RemoveAll(d => d.ImplementationType == typeof(ConsoleLoggerProvider));

        builder.Services.AddSerilog(logger =>
            {
                logger
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .MinimumLevel.Override("System.Net.Http", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("service", serviceName)
                    .Enrich.With<XRayTraceIdEnricher>();

                if (jsonConsole)
                    logger.WriteTo.Console(new ExpressionTemplate(JsonLineTemplate));

                if (!fileEnabled) return;

                logger
                    .WriteTo.File(
                        formatter: new ExpressionTemplate(JsonLineTemplate),
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
                    });
            },
            writeToProviders: true);

        return builder;
    }

    private static void RemoveAll(this IServiceCollection services, Func<ServiceDescriptor, bool> match)
    {
        foreach (var descriptor in services.Where(match).ToList())
            services.Remove(descriptor);
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
