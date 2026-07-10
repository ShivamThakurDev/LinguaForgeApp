using Xunit;

namespace LinguaForge.Tests;

/// <summary>
/// Shares a single <see cref="CustomWebApplicationFactory"/> across all integration test classes
/// and runs them sequentially. The factory sets process-wide environment variables (Jwt__Key, …)
/// in its constructor and clears them on dispose, so parallel factories would race; one shared
/// instance per collection avoids that and is cheaper to spin up.
/// </summary>
[CollectionDefinition(IntegrationCollection.Name)]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "Integration";
}
