using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Shop.Api.Demo;

/// <summary>
/// STEP 1: Declare where your custom telemetry comes from.
///
/// In .NET you don't need OpenTelemetry types to create telemetry. You use the
/// built-in System.Diagnostics APIs:
///   - ActivitySource creates spans (traces)
///   - Meter creates instruments (metrics)
///   - ILogger writes logs
///
/// OpenTelemetry only *listens* to them. The name below must be registered in
/// Program.cs (AddShopTelemetry(..., DemoTelemetry.SourceName)) or nothing is exported.
/// </summary>
public static class DemoTelemetry
{
    public const string SourceName = "Shop.Demo";

    // Create these once and reuse them (static). Never create one per request.
    public static readonly ActivitySource ActivitySource = new(SourceName);
    private static readonly Meter Meter = new(SourceName);

    // --- The four kinds of metric instruments ---

    // Counter: a number that only goes up. "How many times did X happen?"
    public static readonly Counter<long> ButtonClicks = Meter.CreateCounter<long>(
        name: "demo.button.clicks",
        unit: "{click}",
        description: "How many times each demo button was clicked");

    // Histogram: a distribution of values. "How long / how big was X?" (gives avg, p50, p99)
    public static readonly Histogram<double> WorkDuration = Meter.CreateHistogram<double>(
        name: "demo.work.duration",
        unit: "ms",
        description: "How long the simulated work took");

    // UpDownCounter: goes up and down. "How many X are in progress right now?"
    public static readonly UpDownCounter<long> ActiveJobs = Meter.CreateUpDownCounter<long>(
        name: "demo.jobs.active",
        unit: "{job}",
        description: "Demo jobs currently running");

    // ObservableGauge: a value read on demand when metrics are exported. "What is X right now?"
    private static int _queueDepth;
    public static void SetQueueDepth(int value) => Interlocked.Exchange(ref _queueDepth, value);

    static DemoTelemetry()
    {
        Meter.CreateObservableGauge(
            name: "demo.queue.depth",
            observeValue: () => Volatile.Read(ref _queueDepth),
            unit: "{item}",
            description: "Simulated queue depth");
    }
}
