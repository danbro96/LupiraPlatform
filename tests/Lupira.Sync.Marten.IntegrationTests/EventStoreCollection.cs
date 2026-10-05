using Xunit;

namespace Lupira.Sync.Marten.IntegrationTests;

[CollectionDefinition("integration")]
public sealed class EventStoreCollection : ICollectionFixture<EventStoreFixture>;
