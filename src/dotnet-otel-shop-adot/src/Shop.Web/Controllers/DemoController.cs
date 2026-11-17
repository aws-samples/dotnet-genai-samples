using Microsoft.AspNetCore.Mvc;
using Shop.Web.Services;

namespace Shop.Web.Controllers;

/// <summary>
/// "Telemetry Demo" page: one button per signal. Each click starts a trace in Shop.Web,
/// calls the API's /api/demo endpoint, and shows the ids to look up in AWS.
/// </summary>
public class DemoController(ShopApiClient api, ShopperSimulator simulator, ILogger<DemoController> logger) : Controller
{
    private static readonly string[] Signals = ["logs", "traces", "metrics", "error"];

    public IActionResult Index() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(string signal, CancellationToken ct)
    {
        if (!Signals.Contains(signal)) return BadRequest();

        // This log is written by Shop.Web; it shares the trace id with the API's logs.
        logger.LogInformation("Telemetry demo {Signal} requested", signal);

        var result = await api.RunDemoAsync(signal, ct);
        return View("Index", result);
    }

    /// <summary>Runs simulated shoppers to fill logs, metrics, and traces with realistic data.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulate(int shoppers, CancellationToken ct)
    {
        ViewData["Simulation"] = await simulator.RunAsync(Math.Clamp(shoppers, 1, 100), ct);
        return View("Index");
    }
}
