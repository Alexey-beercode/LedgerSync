namespace LedgerSync.Api.MockExternalApi;

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
