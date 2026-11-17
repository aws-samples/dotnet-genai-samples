using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace Shop.Telemetry;

public static class RequestLoggingExtensions
{
    private static readonly string[] QuietPaths = ["/health", "/lib", "/css", "/js", "/favicon"];

    /// <summary>
    /// Writes one summary log per HTTP request, e.g.
    ///   "HTTP GET /Products/Details/p-100 responded 200 in 12 ms"
    /// with fields RequestMethod, RequestPath, StatusCode, Elapsed, SessionId.
    /// 5xx responses are logged as Error and 4xx as Warning. Health checks and static files are skipped.
    /// Call after UseStaticFiles/UseShopSession and before routing.
    /// </summary>
    public static IApplicationBuilder UseShopRequestLogging(this IApplicationBuilder app)
    {
        // Request logging needs Serilog, which is only registered when the log file is enabled.
        if (app.ApplicationServices.GetService<IDiagnosticContext>() is null) return app;

        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0} ms";

            options.GetLevel = (context, _, exception) =>
                QuietPaths.Any(p => context.Request.Path.StartsWithSegments(p)) ? LogEventLevel.Verbose // below the minimum level, so dropped
                : exception is not null || context.Response.StatusCode >= 500 ? LogEventLevel.Error
                : context.Response.StatusCode >= 400 ? LogEventLevel.Warning
                : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnostics, context) =>
            {
                if (context.Items.TryGetValue("ShopSessionId", out var sessionId))
                    diagnostics.Set("SessionId", sessionId);
                if (context.Request.QueryString.HasValue)
                    diagnostics.Set("QueryString", context.Request.QueryString.Value);
                diagnostics.Set("UserAgent", context.Request.Headers.UserAgent.ToString());
            };
        });
    }
}
