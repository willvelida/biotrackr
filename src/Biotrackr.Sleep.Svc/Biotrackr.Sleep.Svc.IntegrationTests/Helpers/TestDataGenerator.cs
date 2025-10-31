using AutoFixture;
using Biotrackr.Sleep.Svc.Models;
using Biotrackr.Sleep.Svc.Models.FitbitEntities;

namespace Biotrackr.Sleep.Svc.IntegrationTests.Helpers
{
    public static class TestDataGenerator
    {
        private static readonly Fixture _fixture = new Fixture();

        public static SleepDocument GenerateSleepDocument()
        {
            return new SleepDocument
            {
                Id = Guid.NewGuid().ToString(),
                Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                DocumentType = "Sleep",
                Sleep = GenerateSleepResponse()
            };
        }

        public static SleepResponse GenerateSleepResponse()
        {
            var sleepData = new List<Models.FitbitEntities.Sleep>
            {
                new Models.FitbitEntities.Sleep
                {
                    DateOfSleep = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"),
                    Duration = _fixture.Create<int>(),
                    Efficiency = _fixture.Create<int>(),
                    EndTime = DateTime.Now,
                    InfoCode = 0,
                    IsMainSleep = true,
                    LogId = _fixture.Create<long>(),
                    MinutesAsleep = 420,
                    MinutesAwake = 45,
                    MinutesAfterWakeup = 0,
                    MinutesToFallAsleep = 15,
                    StartTime = DateTime.Now.AddDays(-1).AddHours(22),
                    TimeInBed = 480,
                    Type = "stages",
                    LogType = "auto_detected",
                    Levels = new Levels
                    {
                        Summary = new Summary
                        {
                            Stages = new Stages
                            {
                                Deep = 90,
                                Light = 180,
                                Rem = 120,
                                Wake = 30
                            },
                            TotalMinutesAsleep = 420,
                            TotalSleepRecords = 1,
                            TotalTimeInBed = 480
                        },
                        Data = new List<SleepData>
                        {
                            new SleepData
                            {
                                DateTime = DateTime.Now.AddDays(-1).AddHours(22),
                                Level = "light",
                                Seconds = 600
                            }
                        },
                        ShortData = new List<SleepData>()
                    }
                }
            };

            return new SleepResponse 
            { 
                Sleep = sleepData,
                Summary = new Summary
                {
                    Stages = new Stages
                    {
                        Deep = 90,
                        Light = 180,
                        Rem = 120,
                        Wake = 30
                    },
                    TotalMinutesAsleep = 420,
                    TotalSleepRecords = 1,
                    TotalTimeInBed = 480
                }
            };
        }

        public static string GenerateDate()
        {
            return DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
        }
    }
}
