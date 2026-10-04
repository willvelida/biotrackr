using Biotrackr.Food.Api.Models.FitbitEntities;

namespace Biotrackr.Food.Api.Models;

public class FoodDocument
{
    public string Id { get; set; } = string.Empty;
    public FoodResponse Food { get; set; } = new();
    public string Date { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Food";
    public string Provider { get; set; } = "Fitbit";
    public int SchemaVersion { get; set; } = 1;
}
