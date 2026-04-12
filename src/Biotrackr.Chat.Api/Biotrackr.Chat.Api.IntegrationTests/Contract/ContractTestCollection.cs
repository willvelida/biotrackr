using Biotrackr.Chat.Api.IntegrationTests.Fixtures;

namespace Biotrackr.Chat.Api.IntegrationTests.Contract;

[CollectionDefinition(nameof(ContractTestCollection))]
public class ContractTestCollection : ICollectionFixture<ChatApiWebApplicationFactory>
{
}
