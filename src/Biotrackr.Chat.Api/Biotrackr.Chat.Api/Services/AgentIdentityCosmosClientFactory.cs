using Biotrackr.Chat.Api.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Biotrackr.Chat.Api.Services
{
    /// <summary>
    /// Creates a CosmosClient authenticated with the agent identity credential.
    /// Configures the credential to use the agent identity and request an app token
    /// (autonomous agent pattern).
    /// </summary>
    public class AgentIdentityCosmosClientFactory : ICosmosClientFactory
    {
        private readonly MicrosoftIdentityTokenCredential _credential;
        private readonly Settings _settings;

        public AgentIdentityCosmosClientFactory(
            MicrosoftIdentityTokenCredential credential,
            IOptions<Settings> options)
        {
            _credential = credential;
            _settings = options.Value;
        }

        public CosmosClient Create()
        {
            _credential.Options.WithAgentIdentity(_settings.AgentIdentityId);
            _credential.Options.RequestAppToken = true;

            return new CosmosClient(_settings.CosmosEndpoint, _credential, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        }
    }
}
