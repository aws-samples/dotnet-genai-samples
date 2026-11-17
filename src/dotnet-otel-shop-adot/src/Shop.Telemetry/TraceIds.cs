using System.Diagnostics;

namespace Shop.Telemetry;

public static class TraceIds
{
    /// <summary>
    /// Converts a W3C trace id (32 hex chars) to the X-Ray display format
    /// "1-{first 8}-{remaining 24}", which is what the X-Ray console search box expects.
    /// </summary>
    public static string ToXRay(ActivityTraceId traceId)
    {
        var hex = traceId.ToHexString();
        return $"1-{hex[..8]}-{hex[8..]}";
    }

    /// <summary>Trace id of the current span, or null outside a trace.</summary>
    public static string? Current => Activity.Current?.TraceId.ToHexString();

    /// <summary>X-Ray-formatted trace id of the current span, or null outside a trace.</summary>
    public static string? CurrentXRay => Activity.Current is { } a ? ToXRay(a.TraceId) : null;
}
