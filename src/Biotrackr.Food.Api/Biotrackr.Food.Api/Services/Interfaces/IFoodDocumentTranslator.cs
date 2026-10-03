using Biotrackr.Food.Api.Models;

namespace Biotrackr.Food.Api.Services.Interfaces;

public interface IFoodDocumentTranslator
{
    /// <summary>
    /// Translates a stored Food document (version 1 Fitbit or version 2 Google) into the public read contract.
    /// </summary>
    FoodDocument Translate(FoodStoredDocument storedDocument);
}
