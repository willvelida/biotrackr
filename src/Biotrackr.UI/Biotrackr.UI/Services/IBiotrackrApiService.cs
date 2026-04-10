using Biotrackr.UI.Models;
using Biotrackr.UI.Models.Activity;
using Biotrackr.UI.Models.Food;
using Biotrackr.UI.Models.Sleep;
using Biotrackr.UI.Models.Vitals;

namespace Biotrackr.UI.Services
{
    public interface IBiotrackrApiService
    {
        Task<PaginatedResponse<ActivityItem>> GetActivitiesAsync(int pageNumber = 1, int pageSize = 20);
        Task<ActivityItem?> GetActivityByDateAsync(string date);
        Task<PaginatedResponse<ActivityItem>> GetActivitiesByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20);

        Task<PaginatedResponse<FoodItem>> GetFoodLogsAsync(int pageNumber = 1, int pageSize = 20);
        Task<FoodItem?> GetFoodLogByDateAsync(string date);
        Task<PaginatedResponse<FoodItem>> GetFoodLogsByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20);

        Task<PaginatedResponse<SleepItem>> GetSleepRecordsAsync(int pageNumber = 1, int pageSize = 20);
        Task<SleepItem?> GetSleepByDateAsync(string date);
        Task<PaginatedResponse<SleepItem>> GetSleepByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20);

        Task<PaginatedResponse<VitalsItem>> GetVitalsRecordsAsync(int pageNumber = 1, int pageSize = 20);
        Task<VitalsItem?> GetVitalsByDateAsync(string date);
        Task<PaginatedResponse<VitalsItem>> GetVitalsByDateRangeAsync(string startDate, string endDate, int pageNumber = 1, int pageSize = 20);
    }
}
