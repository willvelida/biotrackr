using Biotrackr.Vitals.Api.Models;
using Biotrackr.Vitals.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Vitals.Api.EndpointHandlers
{
    public static class VitalsHandlers
    {
        public static async Task<Ok<PaginationResponse<VitalsDocument>>> GetAllVitals(
            ICosmosRepository cosmosRepository,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var vitalsDocuments = await cosmosRepository.GetAllVitalsDocuments(paginationRequest);
            return TypedResults.Ok(vitalsDocuments);
        }

        public static async Task<Results<NotFound, Ok<VitalsDocument>>> GetVitalsByDate(
            ICosmosRepository cosmosRepository,
            string date)
        {
            var vitalsDocument = await cosmosRepository.GetVitalsDocumentByDate(date);
            if (vitalsDocument == null)
            {
                return TypedResults.NotFound();
            }
            return TypedResults.Ok(vitalsDocument);
        }

        public static async Task<Results<BadRequest, Ok<PaginationResponse<VitalsDocument>>>> GetVitalsByDateRange(
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

            var vitalsDocuments = await cosmosRepository.GetVitalsByDateRange(startDate, endDate, paginationRequest);
            return TypedResults.Ok(vitalsDocuments);
        }
    }
}
