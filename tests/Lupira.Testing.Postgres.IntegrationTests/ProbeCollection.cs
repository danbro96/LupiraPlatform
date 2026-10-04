using Xunit;

namespace Lupira.Testing.Postgres.IntegrationTests;

[CollectionDefinition("integration")]
public sealed class ProbeCollection : ICollectionFixture<ProbeFactory>;
