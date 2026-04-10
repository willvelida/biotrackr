using Azure.Security.KeyVault.Secrets;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;
using Biotrackr.Vitals.Svc.Services.Interfaces;
using System.Text.Json;

namespace Biotrackr.Vitals.Svc.Services
{
    public class WithingsService : IWithingsService
    {
        private readonly SecretClient _secretClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<WithingsService> _logger;

        private const string WithingsAccessTokenSecretName = "WithingsAccessToken";
        private const string WithingsMeasureUrl = "https://wbsapi.withings.net/measure";

        public WithingsService(SecretClient secretClient, HttpClient httpClient, ILogger<WithingsService> logger)
        {
            _secretClient = secretClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<WithingsMeasureResponse> GetMeasurements(string startDate, string endDate)
        {
            try
            {
                KeyVaultSecret accessToken = await _secretClient.GetSecretAsync(WithingsAccessTokenSecretName);

                var startUnix = new DateTimeOffset(DateTime.Parse(startDate)).ToUnixTimeSeconds();
                var endUnix = new DateTimeOffset(DateTime.Parse(endDate)).ToUnixTimeSeconds();

                var allMeasureGroups = new List<MeasureGroup>();
                int offset = 0;
                bool hasMore = true;
                string timezone = string.Empty;

                while (hasMore)
                {
                    var response = await FetchMeasurePage(accessToken.Value, startUnix, endUnix, offset);

                    if (response.Body?.MeasureGroups != null)
                    {
                        allMeasureGroups.AddRange(response.Body.MeasureGroups);
                    }

                    if (string.IsNullOrEmpty(timezone) && !string.IsNullOrEmpty(response.Body?.Timezone))
                    {
                        timezone = response.Body.Timezone;
                    }

                    hasMore = response.Body?.More == 1;
                    offset = response.Body?.Offset ?? 0;
                }

                _logger.LogInformation($"Withings API returned {allMeasureGroups.Count} measure group(s)");

                return new WithingsMeasureResponse
                {
                    Status = 0,
                    Body = new WithingsMeasureBody
                    {
                        Timezone = timezone,
                        MeasureGroups = allMeasureGroups,
                        More = 0,
                        Offset = 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception thrown in {nameof(GetMeasurements)}: {ex.Message}");
                throw;
            }
        }

        private async Task<WithingsMeasureResponse> FetchMeasurePage(string accessToken, long startUnix, long endUnix, int offset)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            _logger.LogInformation($"Calling Withings API: startdate={startUnix}, enddate={endUnix}, offset={offset}");

            var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["action"] = "getmeas",
                ["meastypes"] = "1,5,6,8,9,10,11,76,77,88,170",
                ["category"] = "1",
                ["startdate"] = startUnix.ToString(),
                ["enddate"] = endUnix.ToString(),
                ["offset"] = offset.ToString()
            });

            var response = await _httpClient.PostAsync(WithingsMeasureUrl, formContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();

            var measureResponse = JsonSerializer.Deserialize<WithingsMeasureResponse>(content);

            if (measureResponse is null || measureResponse.Status != 0)
            {
                throw new InvalidOperationException(
                    $"Withings Measure API failed. Status: {measureResponse?.Status}. Response: {content}");
            }

            return measureResponse;
        }
    }
}
