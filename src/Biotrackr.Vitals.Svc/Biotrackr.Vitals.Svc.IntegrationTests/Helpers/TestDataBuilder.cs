using System.Net;
using System.Text.Json;
using AutoFixture;
using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;

namespace Biotrackr.Vitals.Svc.IntegrationTests.Helpers
{
    public static class TestDataBuilder
    {
        private static readonly Fixture _fixture = new();

        public static WeightMeasurement BuildWeightMeasurement(DateTime? date = null)
        {
            var testDate = date ?? DateTime.UtcNow;
            return new WeightMeasurement
            {
                Date = testDate.ToString("yyyy-MM-dd"),
                WeightKg = 80.25,
                Bmi = 22.7,
                Fat = 20.5,
                Time = testDate.ToString("HH:mm:ss"),
                Source = "Withings",
                LogId = _fixture.Create<long>(),
                FatMassKg = 15.23,
                FatFreeMassKg = 65.02,
                MuscleMassKg = 45.2,
                BoneMassKg = 3.1,
                WaterMassKg = 48.9,
                VisceralFatIndex = 10
            };
        }

        public static VitalsDocument BuildVitalsDocument(DateTime? date = null)
        {
            var testDate = date ?? DateTime.UtcNow;
            var weight = BuildWeightMeasurement(testDate);
            return new VitalsDocument
            {
                Id = Guid.NewGuid().ToString(),
                Date = testDate.ToString("yyyy-MM-dd"),
                Weight = weight,
                DocumentType = "Vitals",
                Provider = "Withings"
            };
        }

        public static MeasureGroup BuildMeasureGroup(DateTime? date = null)
        {
            var testDate = date ?? DateTime.UtcNow;
            var timestamp = new DateTimeOffset(testDate).ToUnixTimeSeconds();
            return new MeasureGroup
            {
                GrpId = _fixture.Create<long>(),
                Attrib = 0,
                Date = timestamp,
                Created = timestamp + 50,
                Category = 1,
                DeviceId = $"device_{Guid.NewGuid():N}",
                Measures =
                [
                    new Measure { Value = 80250, Type = 1, Unit = -3 },
                    new Measure { Value = 2050, Type = 6, Unit = -2 },
                    new Measure { Value = 15230, Type = 8, Unit = -3 },
                    new Measure { Value = 65020, Type = 5, Unit = -3 },
                    new Measure { Value = 45200, Type = 76, Unit = -3 },
                    new Measure { Value = 3100, Type = 88, Unit = -3 },
                    new Measure { Value = 48900, Type = 77, Unit = -3 },
                    new Measure { Value = 10, Type = 123, Unit = 0 }
                ]
            };
        }

        public static WithingsMeasureResponse BuildWithingsMeasureResponse(int groupCount = 1)
        {
            var groups = Enumerable.Range(0, groupCount)
                .Select(i => BuildMeasureGroup(DateTime.UtcNow.AddDays(-i)))
                .ToList();

            return new WithingsMeasureResponse
            {
                Status = 0,
                Body = new WithingsMeasureBody
                {
                    UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Timezone = "Australia/Sydney",
                    MeasureGroups = groups,
                    More = 0,
                    Offset = 0
                }
            };
        }

        public static Func<HttpRequestMessage, HttpResponseMessage> BuildSuccessfulWithingsResponse(int groupCount = 1)
        {
            return request =>
            {
                var response = BuildWithingsMeasureResponse(groupCount);
                var json = JsonSerializer.Serialize(response);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            };
        }
    }
}
