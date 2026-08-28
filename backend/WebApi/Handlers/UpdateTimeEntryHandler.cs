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

        private readonly ITimeEntryLimitService _limitService;

        public UpdateTimeEntryHandler(ITimeEntryLimitService limitService, IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
            _employees = db.GetCollection<Employee>("Employees");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");

            _limitService = limitService;
        }

        public async Task<bool> Handle(UpdateTimeEntryCommand req, CancellationToken cancellationToken)
        {
            var existing = await _ts.Find(e => e.Id == req.Id).FirstOrDefaultAsync(cancellationToken);
            if (existing == null) return false;

            var startUtc = new DateTime(req.Date.Year, req.Date.Month, req.Date.Day, 0, 0, 0, DateTimeKind.Utc);

            var closed = await _periods.Find(p => p.ClosedAt.Year == startUtc.Year && p.ClosedAt.Month == startUtc.Month)
                                       .FirstOrDefaultAsync(cancellationToken);
            if (closed != null) throw new InvalidOperationException($"Период {startUtc:yyyy-MM} закрыт.");

            var employee = await _employees.Find(e => e.Id == req.EmployeeId).FirstOrDefaultAsync(cancellationToken);
            if (employee == null) throw new InvalidOperationException("Employee not found.");

            var hourlyRate = EmployeeSalaryService.GetRateAt(employee.SalaryHistory, startUtc);
            var expectedCost = Math.Round(hourlyRate * req.Hours, 2);

            var checkResult = await _limitService.CheckHoursLimitAsync(
                req.EmployeeId,
                startUtc,
                req.Hours,
                cancellationToken
            );

            if (!checkResult.IsValid)
            {
                throw new InvalidOperationException(checkResult.ErrorMessage);
            }

            var filter = Builders<TimeEntry>.Filter.Where(e => e.Id == req.Id && e.Version == req.Version);
            var update = Builders<TimeEntry>.Update
                .Set(e => e.Hours, req.Hours)
                .Set(e => e.Comment, req.Comment)
                .Set(e => e.ModifiedBy, req.ModifiedBy ?? "system")
                .Set(e => e.ModifiedAt, req.ModifiedAt)
                .Set(e => e.Date, startUtc)
                .Set(e => e.ProjectId, req.ProjectId)
                .Set(e => e.ExpectedCost, expectedCost)
                .Inc(e => e.Version, 1);

            var result = await _ts.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (result.ModifiedCount == 0)
            {
                throw new InvalidOperationException("Версия записи устарела. Необходимо открыть запись заново.");
            }
            return true;
        }
    }
}