using System;

namespace WebApi.DTO
{
    public class TimeEntryDTO
    {
        public string Id { get; set; }

        public string EmployeeId { get; set; }

        public string ProjectId { get; set; }

        public DateTime Date { get; set; }

        public decimal Hours { get; set; }

        public string Comment { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public string ModifiedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        public int Version { get; set; }
    }
}
