using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Services
{
    public class MongoSeeder
    {
        private readonly IMongoDatabase _db;

        public MongoSeeder(IMongoDatabase db)
        {
            _db = db;
        }

        public async Task SeedAsync()
        {
            var projects = _db.GetCollection<Project>("Projects");
            var employees = _db.GetCollection<Employee>("Employees");
            var periods = _db.GetCollection<ClosedPeriod>("ClosedPeriods");
            var timeEntries = _db.GetCollection<TimeEntry>("TimeEntries");

            // Создание индексов
            await projects.Indexes.CreateOneAsync(new CreateIndexModel<Project>(
                Builders<Project>.IndexKeys.Ascending(p => p.Code),
                new CreateIndexOptions { Unique = true }));

            await employees.Indexes.CreateOneAsync(new CreateIndexModel<Employee>(
                Builders<Employee>.IndexKeys.Ascending(p => p.FullName),
                new CreateIndexOptions { Unique = false }));

            await timeEntries.Indexes.CreateOneAsync(new CreateIndexModel<TimeEntry>(
                Builders<TimeEntry>.IndexKeys
                    .Ascending(e => e.EmployeeId)
                    .Ascending(e => e.ProjectId)
                    .Ascending(e => e.Date),
                new CreateIndexOptions { Unique = false }));

            await periods.Indexes.CreateOneAsync(new CreateIndexModel<ClosedPeriod>(
                Builders<ClosedPeriod>.IndexKeys.Ascending(p => p.Year).Ascending(p => p.Month),
                new CreateIndexOptions { Unique = true }));

            var projectsCount = await projects.EstimatedDocumentCountAsync();
            if (projectsCount == 0)
            {
                var hireRek1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var hireRek2 = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
                var hireIn1 = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

                var seedProjects = new List<Project>
                {
                    new() { Code = "П-001", Name = "Реконструкция цеха", BudgetRub =  20_000m, StartDate = hireRek1, EndDate =  hireRek2},
                    new() { Code = "П-002", Name = "Инженерные сети", BudgetRub = 5_000m, StartDate = hireIn1 }
                };
                await projects.InsertManyAsync(seedProjects);
            }

            var empCount = await employees.EstimatedDocumentCountAsync();
            if (empCount == 0)
            {
                var hireIv1 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var hireIv2 = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
                var hirePet1 = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);

                var seedEmployees = new List<Employee>
                {
                    new() { FullName = "Иванов И. И.", Department = "Проектный",
                        SalaryHistory = new List<SalaryHistory>{ 
                            new() { HourlyRate = 500m, EffectiveFrom = hireIv1 }, 
                            new() { HourlyRate = 600m, EffectiveFrom = hireIv2 } } },
                    new() { FullName = "Петрова А. С.", Department = "Проектный",
                        SalaryHistory = new List<SalaryHistory>{ new() { HourlyRate = 700m, EffectiveFrom = hirePet1 } } },
                };
                await employees.InsertManyAsync(seedEmployees);
            }

            var timeEntriesCount = await timeEntries.EstimatedDocumentCountAsync();
            if (timeEntriesCount == 0)
            {
                var empList = await employees.Find(Builders<Employee>.Filter.Empty).ToListAsync();
                var projList = await projects.Find(Builders<Project>.Filter.Empty).ToListAsync();

                var hire1 = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
                var hire2 = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
                var hire3 = new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc);
                var hire4 = new DateTime(2026, 3, 6, 0, 0, 0, DateTimeKind.Utc);

                var seedList = new List<TimeEntry>
                {
                    new() {
                        EmployeeId = empList[0].Id,
                        ProjectId = projList[0].Id,
                        Date = hire1,
                        Hours = 8m,
                        ExpectedCost = 4_000m,
                        Comment = "Работа над модулем X",
                        CreatedBy = "admin",
                        CreatedAt = DateTime.Now,
                        Version = 1
                    },
                    new() {
                        EmployeeId = empList[0].Id,
                        ProjectId = projList[0].Id,
                        Date = hire2,
                        Hours = 8m,
                        ExpectedCost = 4_800m,
                        Comment = "Работа над модулем X",
                        CreatedBy = "admin",
                        CreatedAt = DateTime.Now,
                        Version = 1
                    },
                    new() {
                        EmployeeId = empList[1].Id,
                        ProjectId = projList[0].Id,
                        Date = hire3,
                        Hours = 4m,
                        ExpectedCost = 2_800m,
                        Comment = "Работа над модулем Y",
                        CreatedBy = "admin",
                        CreatedAt = DateTime.Now,
                        Version = 1
                    },
                    new() {
                        EmployeeId = empList[1].Id,
                        ProjectId = projList[1].Id,
                        Date = hire3,
                        Hours = 10m,
                        ExpectedCost = 7_000m,
                        Comment = "Работа над модулем Z",
                        CreatedBy = "admin",
                        CreatedAt = DateTime.Now,
                        Version = 1
                    },
                };

                await timeEntries.InsertManyAsync(seedList);
            }
        }
    }
}
