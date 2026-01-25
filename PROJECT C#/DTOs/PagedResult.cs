namespace PROJECT_C_.DTOs
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => 
            (int)Math.Ceiling(TotalItems / (double)PageSize);

        public PagedResult(
            List<T> items,
            int page,
            int totalItems,
            int pageSize)
        {
            Items = items;
            Page = page;
            TotalItems = totalItems;
            PageSize = pageSize;
        }
    }
}
