using System.Diagnostics;
using Shop.Contracts;
using Shop.Telemetry;

namespace Shop.Api.Demo;

/// <summary>
/// Small, self-contained examples of adding logs, traces, and metrics.
/// Each endpoint returns the trace id so you can find its telemetry in AWS.
///
///   POST /api/demo/logs     structured logs, scopes, levels
///   POST /api/demo/traces   custom spans, attributes, events, nesting
///   POST /api/demo/metrics  counter, histogram, up/down counter, gauge
///   POST /api/demo/error    what a failure looks like in all three signals
/// </summary>
public static partial class DemoEndpoints
{
    public static void MapDemoEndpoints(this WebApplication app)
    {
        var demo = app.MapGroup("/api/demo");
        demo.MapPost("/logs", Logs);
        demo.MapPost("/traces", Traces);
        demo.MapPost("/metrics", Metrics);
        demo.MapPost("/error", Error);
    }

    // =====================================================================
    // LOGS
    // =====================================================================
    private static IResult Logs(ILogger<DemoLog> logger)
    {
        DemoTelemetry.ButtonClicks.Add(1, new KeyValuePair<string, object?>("button", "logs"));

        // 1. Structured logging: use {Placeholders}, NOT string interpolation ($"...").
        //    Each placeholder becomes its own searchable field (e.g. CustomerId, CartTotal).
        logger.LogInformation("Customer {CustomerId} viewed cart worth {CartTotal}", "cust-42", 129.99m);

        // 2. Log levels: Debug < Information < Warning < Error < Critical.
        //    Pick the level by "would someone need to act on this?"
        logger.LogWarning("Inventory for {ProductId} is low: {Remaining} left", "p-300", 3);

        // 3. Scopes: attach the same fields to every log inside the block.
        using (logger.BeginScope(new Dictionary<string, object> { ["OrderId"] = "ord-demo-123" }))
        {
            logger.LogInformation("Reserving stock");     // has OrderId
            logger.LogInformation("Charging card");       // has OrderId
        }

        // 4. High-performance logging with a source generator (see LogOrderShipped below).
        LogOrderShipped(logger, "ord-demo-123", "UPS");

        // Every log above automatically carries traceId and spanId, so it links to the trace.
        return Results.Ok(Result("logs",
            [
                "Wrote 6 structured log entries (Information and Warning).",
                "Two of them share an OrderId field through a logging scope.",
                "Every entry includes traceId, spanId, and xrayTraceId.",
            ],
            [
                "Log file: /tmp/shop-logs/shop-api.log (one JSON object per line)",
                "CloudWatch Logs > Log groups > /dotnet-shop/api",
                "Logs Insights: fields @timestamp, level, message | filter traceId = \"<trace id>\"",
            ]));
    }

    // Source-generated logging: the compiler writes an efficient method for you.
    // Use this for logs on hot paths that run very often.
    [LoggerMessage(Level = LogLevel.Information, Message = "Order {OrderId} shipped via {Carrier}")]
    private static partial void LogOrderShipped(ILogger logger, string orderId, string carrier);

    // =====================================================================
    // TRACES
    // =====================================================================
    private static async Task<IResult> Traces(ILogger<DemoLog> logger)
    {
        DemoTelemetry.ButtonClicks.Add(1, new KeyValuePair<string, object?>("button", "traces"));

        // 1. Start a span. "using" ends it automatically. StartActivity returns null when
        //    nobody is listening, so always use ?. on it.
        using var workflow = DemoTelemetry.ActivitySource.StartActivity("DemoWorkflow");

        // 2. Attributes (tags): key/value facts about this unit of work. Searchable in X-Ray
        //    when listed under indexed_attributes in the collector config.
        workflow?.SetTag("demo.customer_tier", "gold");
        workflow?.SetTag("demo.item_count", 3);

        // 3. Child spans: started while the parent is current, so they nest under it.
        using (var load = DemoTelemetry.ActivitySource.StartActivity("LoadData"))
        {
            load?.SetTag("demo.source", "catalog");
            await Task.Delay(Random.Shared.Next(40, 120));
        }

        using (var transform = DemoTelemetry.ActivitySource.StartActivity("TransformData"))
        {
            await Task.Delay(Random.Shared.Next(20, 80));

            // 4. Events: a timestamped note inside a span ("something happened at this moment").
            transform?.AddEvent(new ActivityEvent("cache.miss",
                tags: new ActivityTagsCollection { ["demo.key"] = "price:p-300" }));

            await Task.Delay(Random.Shared.Next(20, 80));
        }

        // 5. A span kind tells tools what the span represents. Client = a call to something external.
        using (var call = DemoTelemetry.ActivitySource.StartActivity("CallPricingService", ActivityKind.Client))
        {
            call?.SetTag("peer.service", "pricing-service");
            await Task.Delay(Random.Shared.Next(50, 150));
        }

        logger.LogInformation("Demo workflow finished"); // linked to the same trace
        return Results.Ok(Result("traces",
            [
                "Created a DemoWorkflow span with three child spans: LoadData, TransformData, CallPricingService.",
                "Added attributes (demo.customer_tier, demo.item_count) and a cache.miss event.",
                "The trace starts in Shop.Web, so you see web → API → custom spans as one timeline.",
            ],
            [
                "CloudWatch > Traces: search the X-Ray trace id below",
                "Open the trace to see the waterfall timeline and each span's attributes",
                "Trace Map shows shop-web → shop-api",
            ]));
    }

    // =====================================================================
    // METRICS
    // =====================================================================
    private static async Task<IResult> Metrics()
    {
        // Counter with an attribute (dimension). Each distinct value becomes its own series,
        // so only use attributes with a small set of values (not user ids or order ids).
        DemoTelemetry.ButtonClicks.Add(1, new KeyValuePair<string, object?>("button", "metrics"));

        // UpDownCounter: +1 when work starts, -1 when it ends.
        DemoTelemetry.ActiveJobs.Add(1);
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await Task.Delay(Random.Shared.Next(50, 400));
        }
        finally
        {
            DemoTelemetry.ActiveJobs.Add(-1);
        }

        // Histogram: record how long it took.
        DemoTelemetry.WorkDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("demo.job_type", "report"));

        // Gauge: just set the current value; it's read when metrics are exported.
        var depth = Random.Shared.Next(0, 50);
        DemoTelemetry.SetQueueDepth(depth);

        return Results.Ok(Result("metrics",
            [
                "demo.button.clicks +1 (counter, dimension button=metrics)",
                $"demo.work.duration recorded {stopwatch.Elapsed.TotalMilliseconds:F0} ms (histogram)",
                "demo.jobs.active went +1 then -1 (up/down counter)",
                $"demo.queue.depth is now {depth} (observable gauge)",
            ],
            [
                "Metrics are exported every 60 seconds, so wait about a minute",
                "CloudWatch > Metrics > All metrics > DotnetShop",
                "Click the button a few times to build up a graph",
            ]));
    }

    // =====================================================================
    // ERRORS: how a failure shows up in all three signals
    // =====================================================================
    private static IResult Error(ILogger<DemoLog> logger)
    {
        DemoTelemetry.ButtonClicks.Add(1, new KeyValuePair<string, object?>("button", "error"));

        using var activity = DemoTelemetry.ActivitySource.StartActivity("ProcessRefund");
        activity?.SetTag("demo.refund_id", "rf-demo-9");
        try
        {
            throw new InvalidOperationException("Refund gateway timed out");
        }
        catch (Exception ex)
        {
            // Trace: mark the span as failed and attach the exception (type, message, stack).
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            // Log: pass the exception as the FIRST argument so the stack trace is captured.
            logger.LogError(ex, "Refund {RefundId} failed", "rf-demo-9");
        }

        // The 500 status also marks the server span as an error and shows up in
        // http.server.request.duration with http.response.status_code=500.
        return Results.Json(Result("error",
            [
                "ProcessRefund span marked Error with the exception attached",
                "Error log written with the full stack trace",
                "HTTP 500 returned, so the request counts as a fault",
            ],
            [
                "CloudWatch > Traces: filter by fault/error, or search the trace id",
                "Logs Insights: filter level = \"Error\"",
                "Trace Map: shop-api node shows a red error percentage",
            ]), statusCode: StatusCodes.Status500InternalServerError);
    }

    private static DemoResult Result(string signal, string[] whatHappened, string[] whereToLook) =>
        new(signal, TraceIds.Current ?? "", TraceIds.CurrentXRay ?? "", whatHappened, whereToLook);

    // Category name for the demo logs ("Shop.Api.Demo.DemoLog").
    private sealed class DemoLog;
}
