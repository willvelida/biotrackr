using Biotrackr.Food.Api.Models;

namespace Biotrackr.Food.Api.Repositories.Interfaces;

public interface ICosmosRepository
{
    Task<List<FoodStoredDocument>> GetAllFoodLogsAsync(int pageNumber, int pageSize);
    Task<FoodStoredDocument?> GetFoodLogByDateAsync(string date);
    Task<List<FoodStoredDocument>> GetFoodLogsByDateRangeAsync(string startDate, string endDate, int pageNumber, int pageSize);
    Task<int> GetTotalFoodLogsCountAsync();
    Task<int> GetFoodLogsCountByDateRangeAsync(string startDate, string endDate);
}
