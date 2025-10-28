using System.Net;
using System.Text.Json;
using AutoFixture;
using Biotrackr.Weight.Svc.Models;
using Biotrackr.Weight.Svc.Models.Entities;
using ent = Biotrackr.Weight.Svc.Models.Entities;

namespace Biotrackr.Weight.Svc.IntegrationTests.Helpers
{
    /// <summary>
    /// Builder class for creating test data objects using AutoFixture.
    /// Provides consistent test data generation across all integration tests.
    /// </summary>
    public static class TestDataBuilder
    {
        private static readonly Fixture _fixture = new();

        /// <summary>
        /// Builds a Weight entity with default values.
        /// </summary>
        public static ent.Weight BuildWeight(DateTime? date = null)
        {
            var testDate = date ?? DateTime.UtcNow;
            return new ent.Weight
            {
                Date = testDate.ToString("yyyy-MM-dd"),
                weight = _fixture.Create<double>() % 150 + 50, // 50-200 kg range
                Bmi = _fixture.Create<double>() % 20 + 18, // 18-38 BMI range
                Fat = _fixture.Create<double>() % 30 + 10, // 10-40% range
                Time = testDate.ToString("HH:mm:ss"),
                Source = "API",
                LogId = _fixture.Create<long>()
            };
        }

        /// <summary>
        /// Builds a WeightResponse with specified number of weight entries.
        /// </summary>
        public static WeightResponse BuildWeightResponse(int count = 7)
        {
            var weights = new List<ent.Weight>();
            var startDate = DateTime.UtcNow.AddDays(-count);

            for (int i = 0; i < count; i++)
            {
                weights.Add(BuildWeight(startDate.AddDays(i)));
            }

            return new WeightResponse
            {
                Weight = weights
            };
        }

        /// <summary>
        /// Builds a WeightDocument for database operations.
        /// </summary>
        public static WeightDocument BuildWeightDocument(DateTime? date = null)
        {
            var testDate = date ?? DateTime.UtcNow;
            return new WeightDocument
            {
                Id = Guid.NewGuid().ToString(),
                Date = testDate.ToString("yyyy-MM-dd"),
                Weight = BuildWeight(testDate),
                DocumentType = "Weight"
            };
        }

        /// <summary>
        /// Builds a successful Fitbit API HTTP response with weight data.
        /// </summary>
        public static Func<HttpRequestMessage, HttpResponseMessage> BuildSuccessfulFitbitResponse(int weightCount = 7)
        {
            return request =>
            {
                var response = BuildWeightResponse(weightCount);
                var json = JsonSerializer.Serialize(response);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            };
        }

        /// <summary>
        /// Builds an error Fitbit API HTTP response.
        /// </summary>
        public static Func<HttpRequestMessage, HttpResponseMessage> BuildErrorFitbitResponse(
            HttpStatusCode statusCode = HttpStatusCode.Unauthorized)
        {
            return request => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("{\"errors\":[{\"errorType\":\"oauth\",\"message\":\"Access token invalid\"}]}")
            };
        }

        /// <summary>
        /// Builds an empty Fitbit API HTTP response (no weight data).
        /// </summary>
        public static Func<HttpRequestMessage, HttpResponseMessage> BuildEmptyFitbitResponse()
        {
            return request => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"weight\":[]}")
            };
        }

        /// <summary>
        /// Builds a timeout scenario for testing error handling.
        /// </summary>
        public static Func<HttpRequestMessage, HttpResponseMessage> BuildTimeoutResponse()
        {
            return request => throw new TaskCanceledException("The request timed out");
        }
    }
}
