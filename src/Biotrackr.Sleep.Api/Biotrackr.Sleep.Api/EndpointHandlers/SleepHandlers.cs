using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using Biotrackr.Sleep.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Biotrackr.Sleep.Api.EndpointHandlers
{
    public static class SleepHandlers
    {
        public static async Task<Results<NotFound, Ok<SleepDocument>>> GetSleepByDate(
            ICosmosRepository cosmosRepository,
            ISleepDocumentTranslator sleepDocumentTranslator,
            string date)
        {
            var sleep = await cosmosRepository.GetSleepSummaryByDate(date);
            if (sleep == null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(sleepDocumentTranslator.Translate(sleep));
        }

        public static async Task<Ok<PaginationResponse<SleepDocument>>> GetAllSleeps(
            ICosmosRepository cosmosRepository,
            ISleepDocumentTranslator sleepDocumentTranslator,
            int? pageNumber = null,
            int? pageSize = null)
        {
            var paginationRequest = new PaginationRequest
            {
                PageNumber = pageNumber ?? 1,
                PageSize = pageSize ?? 20
            };

            var sleeps = await cosmosRepository.GetAllSleepDocuments(paginationRequest);
            return TypedResults.Ok(Translate(sleeps, sleepDocumentTranslator));
        }

        public static async Task<Results<BadRequest, Ok<PaginationResponse<SleepDocument>>>> GetSleepsByDateRange(
            ICosmosRepository cosmosRepository,
            ISleepDocumentTranslator sleepDocumentTranslator,
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

            var sleepDocuments = await cosmosRepository.GetSleepDocumentsByDateRange(startDate, endDate, paginationRequest);
            return TypedResults.Ok(Translate(sleepDocuments, sleepDocumentTranslator));
        }

        // O(n) over the page; storage models never leave the API (raw Google payloads are Newtonsoft JObjects).
        private static PaginationResponse<SleepDocument> Translate(
            PaginationResponse<SleepStoredDocument> storedPage,
            ISleepDocumentTranslator sleepDocumentTranslator)
        {
            var items = new List<SleepDocument>(storedPage.Items.Count);
            foreach (var storedDocument in storedPage.Items)
            {
                items.Add(sleepDocumentTranslator.Translate(storedDocument));
            }

            return new PaginationResponse<SleepDocument>
            {
                Items = items,
                TotalCount = storedPage.TotalCount,
                PageNumber = storedPage.PageNumber,
                PageSize = storedPage.PageSize
            };
        }
    }
}
