using System.Collections.Generic;

namespace WebApi.DTO
{
    public class EmployeeDTO
    {
        public string Id { get; set; }

        public string FullName { get; set; }

        public string Department { get; set; }

        public decimal CurrentHourlyRate { get; set; }

        public List<SalaryHistoryDTO> SalaryHistory { get; set; }
    }
}
