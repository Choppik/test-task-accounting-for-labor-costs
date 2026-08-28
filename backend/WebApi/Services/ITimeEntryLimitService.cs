using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Services
{
    public interface ITimeEntryLimitService
    {
        Task<TimeEntryCheckResult> CheckHoursLimitAsync(
            string employeeId,
            DateTime startUtc,
            decimal newHours,
            CancellationToken cancellationToken);
    }

    public class TimeEntryCheckResult
    {
        public double ExistingHours { get; set; }
        public double TotalHoursAfterAdd { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
