using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;
using WebApi.Commands;
using WebApi.Models;

namespace WebApi.Handlers.Timesheets
{
    public class DeleteTimeEntryHandler : IRequestHandler<DeleteTimeEntryCommand, bool>
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<ClosedPeriod> _periods;

        public DeleteTimeEntryHandler(IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");
        }

        public async Task<bool> Handle(DeleteTimeEntryCommand req, CancellationToken cancellationToken)
        {
            var existing = await _ts.Find(e => e.Id == req.Id).FirstOrDefaultAsync(cancellationToken);
            if (existing == null) return false;

            var date = existing.Date;
            var closed = await _periods.Find(p => p.ClosedAt.Year == date.Year && p.ClosedAt.Month == date.Month)
                                       .FirstOrDefaultAsync(cancellationToken);
            if (closed != null) throw new InvalidOperationException($"Период {date:yyyy-MM} закрыт.");

            var res = await _ts.DeleteOneAsync(e => e.Id == req.Id, cancellationToken);
            return res.DeletedCount > 0;
        }
    }
}