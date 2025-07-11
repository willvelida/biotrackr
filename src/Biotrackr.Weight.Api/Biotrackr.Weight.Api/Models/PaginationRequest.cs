namespace Biotrackr.Weight.Api.Models
{
    public class PaginationRequest
    {
        private int _pageNumber = 1;
        private int _pageSize = 20;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value < 1 ? 20 : value > 100 ? 100 : value;
        }

        public int Skip => (PageNumber - 1) * PageSize;
    }

    public class PaginationResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
