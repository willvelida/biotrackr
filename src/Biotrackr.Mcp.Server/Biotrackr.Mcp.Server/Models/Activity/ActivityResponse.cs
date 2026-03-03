using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Biotrackr.Mcp.Server.Models.Activity
{
    public class ActivityResponse
    {
        [JsonPropertyName("items")]
        public List<ActivityItem> Items { get; set; } = [];

        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }
}
