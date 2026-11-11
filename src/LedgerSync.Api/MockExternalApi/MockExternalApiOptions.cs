namespace LedgerSync.Api.MockExternalApi;

/// <summary>
/// Settings of the simulated external system. Section: <c>MockExternalApi</c>.
/// Disabled by default: the mock must never be reachable in production.
/// </summary>
public sealed class MockExternalApiOptions
{
    public const string SectionName = "MockExternalApi";

    public bool Enabled { get; init; }
}
