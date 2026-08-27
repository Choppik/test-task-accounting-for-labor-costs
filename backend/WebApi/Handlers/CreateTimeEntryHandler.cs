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
    public class CreateTimetEntryHandler : IRequestHandler<CreateTimeEntryCommand, string>
    {
        private readonly IMongoCollection<TimeEntry> _collection;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<ClosedPeriod> _periods;

        public CreateTimetEntryHandler(IMongoDatabase db)
        {
            _collection = db.GetCollection<TimeEntry>("TimeEntries");
            _employees = db.GetCollection<Employee>("Employees");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");
        }

        public async Task<string> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
        {
            var entryDate = request.Date.Date;

            var year = entryDate.Year;
            var month = entryDate.Month;
            var closed = await _periods.Find(p => p.Year == year && p.Month == month && p.IsClosed)
                                       .FirstOrDefaultAsync(cancellationToken);
            if (closed != null)
                throw new InvalidOperationException($"Period {year}-{month} is closed.");

            var employee = await _employees.Find(e => e.Id == request.EmployeeId).FirstOrDefaultAsync(cancellationToken);
            if (employee == null)
                throw new InvalidOperationException("Employee not found.");

            var hourlyRate = EmployeeSalaryService.GetRateAt(employee.SalaryHistory, entryDate);
            var expectedCost = Math.Round(hourlyRate * request.Hours, 2);

            var now = DateTime.UtcNow;
            var entry = new TimeEntry
            {
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                Date = entryDate.ToUniversalTime(),
                Hours = request.Hours,
                ExpectedCost = expectedCost,
                Comment = request.Comment,
                CreatedBy = request.CreatedBy ?? "system",
                CreatedAt = now,
                Version = 1
            };

            await _collection.InsertOneAsync(entry, cancellationToken: cancellationToken);
            return entry.Id;
        }
    }
}