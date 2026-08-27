using System;

namespace WebApi.DTO
{
    public class ProjectReportItem
    {
        public string Id { get; set; }
        public string ProjectCode { get; set; }
        public string ProjectName { get; set; }
        public decimal BudgetRub { get; set; }
        public int TotalHours { get; set; }
        public decimal TotalCost { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public decimal PercentSpent { get; set; }
    }
}
