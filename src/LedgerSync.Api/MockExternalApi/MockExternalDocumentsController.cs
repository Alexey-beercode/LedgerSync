using Microsoft.AspNetCore.Mvc;

namespace LedgerSync.Api.MockExternalApi;

/// <summary>
/// Simulated external documents system. Lives in the same process only for demo purposes;
/// the sync service reaches it over real HTTP, like any other external API.
/// </summary>
[ApiController]
[Route("mock/external")]
[Tags("Mock External API")]
public sealed class MockExternalDocumentsController(
    MockExternalScenarioState state,
    IWebHostEnvironment environment) : ControllerBase
{
    /// <summary>Returns documents according to the active scenario.</summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
    {
        var scenario = state.Current;
        if (scenario == MockExternalScenario.Unavailable)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "External system is down (simulated)." });

        var json = await System.IO.File.ReadAllTextAsync(DataFile(scenario), cancellationToken);
        return Content(json, "application/json");
    }

    /// <summary>Returns the active scenario.</summary>
    [HttpGet("scenario")]
    public IActionResult GetScenario() =>
        Ok(new { scenario = state.Current.ToString(), available = Enum.GetNames<MockExternalScenario>() });

    /// <summary>Switches the active scenario (Baseline, Updated, Mixed, Stale, Malformed, Unavailable).</summary>
    [HttpPut("scenario/{scenario}")]
    public IActionResult SetScenario(MockExternalScenario scenario)
    {
        if (!Enum.IsDefined(scenario))
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Unknown scenario '{scenario}'.");

        state.Current = scenario;
        return Ok(new { scenario = scenario.ToString() });
    }

    private string DataFile(MockExternalScenario scenario) =>
        Path.Combine(environment.ContentRootPath, "MockExternalApi", "Data", $"{scenario.ToString().ToLowerInvariant()}.json");
}
