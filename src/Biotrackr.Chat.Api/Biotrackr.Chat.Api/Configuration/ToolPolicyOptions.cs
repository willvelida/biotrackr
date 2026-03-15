namespace Biotrackr.Chat.Api.Configuration
{
    public class ToolPolicyOptions
    {
        public int MaxToolCallsPerSession { get; set; } = 20;
        public HashSet<string> AllowedToolNames { get; set; } =
        [
            "GetActivityByDate", "GetActivityByDateRange", "GetActivityRecords",
            "GetSleepByDate", "GetSleepByDateRange", "GetSleepRecords",
            "GetWeightByDate", "GetWeightByDateRange", "GetWeightRecords",
            "GetFoodByDate", "GetFoodByDateRange", "GetFoodRecords"
        ];
    }
}
