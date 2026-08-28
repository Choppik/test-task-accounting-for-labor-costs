using MediatR;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Commands;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Handlers.Timesheets
{
    public class CreateTimetEntryHandler : IRequestHandler<CreateTimeEntryCommand, string>
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<ClosedPeriod> _periods;
        private readonly IMongoCollection<Project> _projects;

        private readonly ITimeEntryLimitService _limitService;

        public CreateTimetEntryHandler(ITimeEntryLimitService limitService, IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
            _employees = db.GetCollection<Employee>("Employees");
            _projects = db.GetCollection<Project>("Projects");
            _periods = db.GetCollection<ClosedPeriod>("ClosedPeriods");

            _limitService = limitService;
        }

        public async Task<string> Handle(CreateTimeEntryCommand request, CancellationToken cancellationToken)
        {
            // Нормализуем дату запроса к 00:00 UTC
            var startUtc = new DateTime(request.Date.Year, request.Date.Month, request.Date.Day, 0, 0, 0, DateTimeKind.Utc);

            var year = startUtc.Year;
            var month = startUtc.Month;

            var closed = await _periods.Find(p => p.ClosedAt.Year == year && p.ClosedAt.Month == month)
                                       .FirstOrDefaultAsync(cancellationToken);
            if (closed != null)
                throw new InvalidOperationException($"Период {startUtc:yyyy-MM} закрыт.");

            var employee = await _employees.Find(e => e.Id == request.EmployeeId).FirstOrDefaultAsync(cancellationToken);
            if (employee == null)
                throw new InvalidOperationException("Employee not found.");

            var hourlyRate = EmployeeSalaryService.GetRateAt(employee.SalaryHistory, startUtc);
            if (hourlyRate == 0)
            {
                throw new InvalidOperationException(
                    $"Невозможно создать запись табеля на дату {startUtc:yyyy-MM-dd}: " +
                    $"у сотрудника {employee.FullName} нет действующей ставки на этот период. " +
                    "Добавьте или обновите историю ставок."
                );
            }

            var project = await _projects.Find(e => e.Id == request.ProjectId).FirstOrDefaultAsync(cancellationToken);
            if (project == null)
                throw new InvalidOperationException("Project not found.");
            var endStr = project.EndDate?.ToString("yyyy-MM-dd") ?? "(БЕССРОЧНО)";
            if (project.StartDate < startUtc && project.EndDate != null && project.EndDate < startUtc)
                throw new InvalidOperationException($"Выбрана дата вне диапазона проекта ({project.StartDate:yyyy-MM-dd} - {endStr}).");

            var checkResult = await _limitService.CheckHoursLimitAsync(
                request.EmployeeId,
                startUtc,
                request.Hours,
                cancellationToken
            );

            if (!checkResult.IsValid)
            {
                throw new InvalidOperationException(checkResult.ErrorMessage);
            }

            var expectedCost = Math.Round(hourlyRate * request.Hours, 2);

            var entry = new TimeEntry
            {
                EmployeeId = request.EmployeeId,
                ProjectId = request.ProjectId,
                Date = startUtc,
                Hours = request.Hours,
                ExpectedCost = expectedCost,
                Comment = request.Comment,
                CreatedBy = request.CreatedBy ?? "system",
                CreatedAt = request.CreatedAt,
                Version = 1
            };

            await _ts.InsertOneAsync(entry, cancellationToken: cancellationToken);
            return entry.Id;
        }
    }
}