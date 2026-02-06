namespace PROJECT_C_.DTOs
{
    public class ApiResponse<T>
    {
        public int Total { get; set; }
        public T Data { get; set; } = default!;
    }
}
