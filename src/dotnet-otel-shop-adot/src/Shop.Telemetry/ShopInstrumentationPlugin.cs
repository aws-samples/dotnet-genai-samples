using Microsoft.AspNetCore.Http;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Shop.Telemetry;

/// <summary>
/// Optional plugin for the ADOT .NET auto-instrumentation, and the only telemetry code left in
/// the app. It does not create a tracer, a meter, or an exporter; the auto-instrumentation does
/// all of that. It only tells the ASP.NET Core instrumentation which requests to skip, so load
/// balancer health checks and static files don't become spans or Application Signals operations.
///
/// It is loaded by name, never referenced from code. List it after the ADOT plugin
/// (entries are colon-separated, each "Type, Assembly"):
///
///   OTEL_DOTNET_AUTO_PLUGINS=AWS.Distro.OpenTelemetry.AutoInstrumentation.Plugin, AWS.Distro.OpenTelemetry.AutoInstrumentation:Shop.Telemetry.ShopInstrumentationPlugin, Shop.Telemetry
///
/// Leave it out and everything still works; you just also see GET /health and GET /css/... .
///
/// Readable operation names for the MVC storefront need no code: they come from
/// OTEL_AWS_HTTP_OPERATION_PATHS (see scripts/run-local.sh).
/// </summary>
public sealed class ShopInstrumentationPlugin
{
    private static readonly string[] IgnoredPaths = ["/health", "/lib", "/css", "/js", "/favicon"];

    /// <summary>Called by the auto-instrumentation while it configures ASP.NET Core tracing.</summary>
    public void ConfigureTracesOptions(AspNetCoreTraceInstrumentationOptions options)
    {
        // ADOT's plugin may already have set a filter; keep its decision too.
        var previousFilter = options.Filter;
        options.Filter = context =>
            (previousFilter?.Invoke(context) ?? true) &&
            !IgnoredPaths.Any(p => context.Request.Path.StartsWithSegments(p));

        options.RecordException = true;
    }
}
