namespace LedgerSync.Api.MockExternalApi;

/// <summary>
/// Currently active scenario. Singleton, starts with <see cref="MockExternalScenario.Baseline"/>
/// and is switched at runtime via <c>PUT /mock/external/scenario/{scenario}</c>.
/// </summary>
public sealed class MockExternalScenarioState
{
    private volatile MockExternalScenario _current = MockExternalScenario.Baseline;

    public MockExternalScenario Current
    {
        get => _current;
        set => _current = value;
    }
}
