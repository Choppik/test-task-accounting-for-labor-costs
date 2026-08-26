using System;

namespace WebApi.DTO
{
    public class SalaryHistoryDTO
    {
        public decimal HourlyRate { get; set; }
        public DateTime EffectiveFrom { get; set; }
    }
}
