using Azure.Security.KeyVault.Secrets;
using Biotrackr.FitbitApi.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Biotrackr.FitbitApi.Services
{
    public class SecretService : ISecretService
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<SecretService> _logger;

        public SecretService(SecretClient secretClient, ILogger<SecretService> logger)
        {
            _secretClient = secretClient;
            _logger = logger;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                KeyVaultSecret secret = await _secretClient.GetSecretAsync(secretName);
                return secret.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetSecretAsync)}: {ex.Message}");
                throw;
            }
        }
    }
}
