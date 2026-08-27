using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;
using WebApi.Commands;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Handlers.Timesheets
{
    public class UpdateTimeEntryHandler : IRequestHandler<UpdateTimeEntryCommand, bool>
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<ClosedPeriod> _periods;

        public UpdateTimeEntryHandler(IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
            _employees = db.GetCollection<Employee>("Employees");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");
        }

        public async Task<bool> Handle(UpdateTimeEntryCommand req, CancellationToken cancellationToken)
        {
            var existing = await _ts.Find(e => e.Id == req.Id).FirstOrDefaultAsync(cancellationToken);
            if (existing == null) return false;

            var entryDate = req.Date.Date;

            var closed = await _periods.Find(p => p.Year == entryDate.Year && p.Month == entryDate.Month && p.IsClosed)
                                       .FirstOrDefaultAsync(cancellationToken);
            if (closed != null) throw new InvalidOperationException("Period is closed.");

            var employee = await _employees.Find(e => e.Id == req.EmployeeId).FirstOrDefaultAsync(cancellationToken);
            if (employee == null) throw new InvalidOperationException("Employee not found.");

            var hourlyRate = EmployeeSalaryService.GetRateAt(employee.SalaryHistory, entryDate);
            var expectedCost = Math.Round(hourlyRate * req.Hours, 2);

            var filter = Builders<TimeEntry>.Filter.Where(e => e.Id == req.Id && e.Version == req.Version);
            var update = Builders<TimeEntry>.Update
                .Set(e => e.Hours, req.Hours)
                .Set(e => e.Comment, req.Comment)
                .Set(e => e.ModifiedBy, req.ModifiedBy ?? "system")
                .Set(e => e.ModifiedAt, req.ModifiedAt)
                .Set(e => e.Date, entryDate.ToUniversalTime())
                .Set(e => e.ProjectId, req.ProjectId)
                .Set(e => e.ExpectedCost, expectedCost)
                .Inc(e => e.Version, 1);

            var result = await _ts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
                throw new InvalidOperationException("Update failed due to concurrent modification or stale version.");
            }
            return true;
        }
    }
}