namespace WebApi.DTO
{
    public class ReportDTO
    {
        public string Year { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string? Q { get; set; } // поиск
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
