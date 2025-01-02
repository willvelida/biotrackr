using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Weight.Api.EndpointHandlers
{
    public static class WeightHandlers
    {
        public static async Task<Ok<List<WeightDocument>>> GetAllWeights(
            ICosmosRepository cosmosRepository)
        {
            var weightDocuments = await cosmosRepository.GetAllWeightDocuments();
            return TypedResults.Ok(weightDocuments);
        }
    }
}
