namespace LedgerSync.Api.Extensions;

/// <summary>
/// Health check tags.
/// </summary>
internal static class HealthTags
{
    /// <summary>Dependencies required to serve traffic (database).</summary>
    public const string Ready = "ready";
}
