using System;

namespace WebApi.DTO
{
    public class ClosedPeriodDTO
    {
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public bool IsClosed { get; set; }

        public string ClosedBy { get; set; }

        public DateTime ClosedAt { get; set; }
    }
}
