using WebApi.Models;

namespace WebApi.DTO
{
    public class TimeEntryMapper
    {
        public static TimeEntryDTO ToDTO(TimeEntry src)
        {
            if (src == null) return null;

            return new TimeEntryDTO
            {
                Id = src.Id,
                EmployeeId = src.EmployeeId,
                ProjectId = src.ProjectId,
                Date = src.Date.ToLocalTime(),
                Hours = src.Hours,
                ExpectedCost = src.ExpectedCost,
                Comment = src.Comment,
                CreatedBy = src.CreatedBy,
                CreatedAt = src.CreatedAt.ToLocalTime(),
                ModifiedBy = src.ModifiedBy,
                ModifiedAt = src.ModifiedAt?.ToLocalTime(),
                Version = src.Version
            };
        }
    }
}
