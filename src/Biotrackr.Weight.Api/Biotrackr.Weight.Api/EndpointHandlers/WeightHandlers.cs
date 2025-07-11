using Biotrackr.Weight.Api.Models;
using Biotrackr.Weight.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Weight.Api.EndpointHandlers
{
    public static class WeightHandlers
    {
        public static async Task<Ok<PaginationResponse<WeightDocument>>> GetAllWeights(
            ICosmosRepository cosmosRepository,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var weightDocuments = await cosmosRepository.GetAllWeightDocuments(paginationRequest);
            return TypedResults.Ok(weightDocuments);
        }

        public static async Task<Results<NotFound, Ok<WeightDocument>>> GetWeightByDate(
            ICosmosRepository cosmosRepository,
            string date)
        {
            var weightDocument = await cosmosRepository.GetWeightDocumentByDate(date);
            if (weightDocument == null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(weightDocument);
        }
    }
}
