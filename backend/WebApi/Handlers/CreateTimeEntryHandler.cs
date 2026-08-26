using MediatR;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Commands
{
    public class CreateTimeEntryHandler : IRequestHandler<CreateTimeEntryCommand, string>
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<ClosedPeriod> _periods;

        public CreateTimeEntryHandler(IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimesheetEntries");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");
        }

        public async Task<string> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
        {
            var date = request.Date.Date;
            var year = date.Year;
            var month = date.Month;
            var closed = await _periods.Find(p => p.Year == year && p.Month == month && p.IsClosed).FirstOrDefaultAsync(cancellationToken);
            if (closed != null)
                throw new InvalidOperationException($"Period {year}-{month} is closed.");

            var now = DateTime.UtcNow;
            var entry = new TimeEntry
            {
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                Date = date.ToUniversalTime(),
                Hours = request.Hours,
                Comment = request.Comment,
                CreatedBy = request.CreatedBy ?? "system",
                CreatedAt = now,
                Version = 1
            };

            await _ts.InsertOneAsync(entry, cancellationToken: cancellationToken);
            return entry.Id;
        }
    }
}
