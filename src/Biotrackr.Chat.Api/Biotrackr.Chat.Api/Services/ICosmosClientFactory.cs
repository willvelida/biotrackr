using Microsoft.Azure.Cosmos;

namespace Biotrackr.Chat.Api.Services
{
    public interface ICosmosClientFactory
    {
        CosmosClient Create();
    }
}
