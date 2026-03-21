namespace Biotrackr.Chat.Api.Configuration
{
    public class ToolPolicyOptions
    {
        public int MaxToolCallsPerSession { get; set; } = 20;
        public HashSet<string> AllowedToolNames { get; set; } =
        [
            "get_activity_by_date", "get_activity_by_date_range", "get_activity_records",
            "get_sleep_by_date", "get_sleep_by_date_range", "get_sleep_records",
            "get_weight_by_date", "get_weight_by_date_range", "get_weight_records",
            "get_food_by_date", "get_food_by_date_range", "get_food_records"
        ];
    }
}
