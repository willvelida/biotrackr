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

        public static async Task<Results<BadRequest, Ok<PaginationResponse<WeightDocument>>>> GetWeightsByDateRange(
            ICosmosRepository cosmosRepository,
            string startDate,
            string endDate,
            int? pageNumber = null,
            int? pageSize = null)
        {
            // Validate date formats
            if (!DateOnly.TryParse(startDate, out var parsedStartDate) ||
                !DateOnly.TryParse(endDate, out var parsedEndDate))
            {
                return TypedResults.BadRequest();
            }

            // Validate date range (start date should be before or equal to end date)
            if (parsedStartDate > parsedEndDate)
            {
                return TypedResults.BadRequest();
            }

            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var weightDocuments = await cosmosRepository.GetWeightsByDateRange(startDate, endDate, paginationRequest);
            return TypedResults.Ok(weightDocuments);
        }
    }
}
