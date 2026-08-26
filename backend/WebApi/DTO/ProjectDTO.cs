using System;

namespace WebApi.DTO
{
    public class ProjectDTO
    {
        public string Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public decimal BudgetRub { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}
