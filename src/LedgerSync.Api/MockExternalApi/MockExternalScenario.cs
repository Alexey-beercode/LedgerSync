namespace LedgerSync.Api.MockExternalApi;

/// <summary>
/// Behaviour of the simulated external documents system.
/// </summary>
public enum MockExternalScenario
{
    /// <summary>Four valid documents. Second run with the same data creates nothing.</summary>
    Baseline,

    /// <summary>Baseline with changed statuses/amount plus one new document.</summary>
    Updated,

    /// <summary>Valid documents mixed with an unknown order, invalid fields, unreadable item and an in-batch duplicate.</summary>
    Mixed,

    /// <summary>Older version of a known document (out-of-order delivery).</summary>
    Stale,

    /// <summary>Body is not valid JSON.</summary>
    Malformed,

    /// <summary>Source responds with HTTP 503.</summary>
    Unavailable
}

/// <summary>
/// Currently active scenario. Singleton, switchable at runtime via the mock API.
/// </summary>
public sealed class MockExternalScenarioState(MockExternalScenario initial)
{
    private volatile MockExternalScenario _current = initial;

    public MockExternalScenario Current
    {
        get => _current;
        set => _current = value;
    }
}
