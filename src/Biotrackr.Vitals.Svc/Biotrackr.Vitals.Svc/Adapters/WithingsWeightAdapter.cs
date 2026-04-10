using Biotrackr.Vitals.Svc.Models;
using Biotrackr.Vitals.Svc.Models.WithingsEntities;

namespace Biotrackr.Vitals.Svc.Adapters
{
    public static class WithingsWeightAdapter
    {
        public static WeightMeasurement FromMeasureGroup(MeasureGroup grp, double userHeight, TimeZoneInfo userTimezone)
        {
            var measures = grp.Measures.ToDictionary(m => m.Type, m => m);
            var utc = DateTimeOffset.FromUnixTimeSeconds(grp.Date);
            var local = TimeZoneInfo.ConvertTime(utc, userTimezone);

            double weightKg = GetValue(measures, 1);
            double bmi = userHeight > 0 ? Math.Round(weightKg / (userHeight * userHeight), 1) : 0;

            return new WeightMeasurement
            {
                WeightKg = weightKg,
                Bmi = bmi,
                Fat = GetValue(measures, 6),
                Date = local.ToString("yyyy-MM-dd"),
                Time = local.ToString("HH:mm:ss"),
                Source = "Withings",
                LogId = grp.GrpId,
                FatMassKg = GetNullableValue(measures, 8),
                FatFreeMassKg = GetNullableValue(measures, 5),
                MuscleMassKg = GetNullableValue(measures, 76),
                BoneMassKg = GetNullableValue(measures, 88),
                WaterMassKg = GetNullableValue(measures, 77),
                VisceralFatIndex = GetNullableIntValue(measures, 170)
            };
        }

        private static double GetValue(Dictionary<int, Measure> measures, int type)
            => measures.TryGetValue(type, out var v) ? v.Value * Math.Pow(10, v.Unit) : 0;

        private static double? GetNullableValue(Dictionary<int, Measure> measures, int type)
            => measures.TryGetValue(type, out var v) ? v.Value * Math.Pow(10, v.Unit) : null;

        private static int? GetNullableIntValue(Dictionary<int, Measure> measures, int type)
            => measures.TryGetValue(type, out var v) ? (int)(v.Value * Math.Pow(10, v.Unit)) : null;
    }
}
