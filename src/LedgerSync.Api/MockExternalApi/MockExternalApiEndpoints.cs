namespace LedgerSync.Api.MockExternalApi;

/// <summary>
/// Simulated external documents system. Mapped only when <c>MockExternalApi:Enabled</c> is true.
/// The sync handler reaches it over real HTTP, like any other external API.
/// </summary>
public static class MockExternalApiEndpoints
{
    public static IEndpointRouteBuilder MapMockExternalApi(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/mock/external").WithTags("Mock External API");

        group.MapGet("/documents", GetDocumentsAsync)
            .WithSummary("Returns documents according to the active scenario.");

        group.MapGet("/scenario", (MockExternalScenarioState state) =>
                Results.Ok(new { scenario = state.Current.ToString(), available = Enum.GetNames<MockExternalScenario>() }))
            .WithSummary("Returns the active scenario.");

        group.MapPut("/scenario/{scenario}", SetScenario)
            .WithSummary("Switches the active scenario: Baseline, Updated, Mixed, Stale, Malformed, Unavailable.");

        return app;
    }

    private static async Task<IResult> GetDocumentsAsync(
        MockExternalScenarioState state,
        IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var scenario = state.Current;
        if (scenario == MockExternalScenario.Unavailable)
            return Results.Json(new { error = "External system is down (simulated)." },
                statusCode: StatusCodes.Status503ServiceUnavailable);

        var path = Path.Combine(environment.ContentRootPath, "MockExternalApi", "Data",
            $"{scenario.ToString().ToLowerInvariant()}.json");
        var json = await File.ReadAllTextAsync(path, cancellationToken);

        return Results.Content(json, "application/json");
    }

    private static IResult SetScenario(string scenario, MockExternalScenarioState state)
    {
        if (!Enum.TryParse<MockExternalScenario>(scenario, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, detail: $"Unknown scenario '{scenario}'.");

        state.Current = parsed;
        return Results.Ok(new { scenario = parsed.ToString() });
    }
}
