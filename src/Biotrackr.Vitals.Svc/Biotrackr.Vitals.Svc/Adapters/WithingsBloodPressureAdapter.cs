using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;

namespace Biotrackr.Vitals.Svc.Adapters
{
    public static class WithingsBloodPressureAdapter
    {
        public static BloodPressureReading FromMeasureGroup(MeasureGroup grp)
        {
            var measures = grp.Measures.ToDictionary(m => m.Type, m => m);

            return new BloodPressureReading
            {
                Systolic = GetIntValue(measures, 10),
                Diastolic = GetIntValue(measures, 9),
                HeartRate = GetIntValue(measures, 11),
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(grp.Date).ToString("o"),
                Time = DateTimeOffset.FromUnixTimeSeconds(grp.Date).ToString("HH:mm:ss"),
                Source = "Withings",
                LogId = grp.GrpId,
                DeviceId = string.IsNullOrEmpty(grp.DeviceId) ? null : grp.DeviceId
            };
        }

        private static int GetIntValue(Dictionary<int, Measure> measures, int type)
            => measures.TryGetValue(type, out var v) ? (int)(v.Value * Math.Pow(10, v.Unit)) : 0;
    }
}
