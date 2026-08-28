using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Services
{
    public class TimeEntryLimitService : ITimeEntryLimitService
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private const double MaxHoursPerDay = 24.0;

        public TimeEntryLimitService(IMongoCollection<TimeEntry> ts)
        {
            _ts = ts;
        }

        public async Task<TimeEntryCheckResult> CheckHoursLimitAsync(
            string employeeId,
            DateTime startUtc,
            decimal newHours,
            CancellationToken cancellationToken)
        {
            if (!ObjectId.TryParse(employeeId, out var employeeIdOid))
            {
                return new TimeEntryCheckResult
                {
                    IsValid = false,
                    ErrorMessage = $"Неверный формат EmployeeId: '{employeeId}'. Ожидается ObjectId."
                };
            }

            var employeeIdFilter = (BsonValue)employeeIdOid;

            var endUtc = startUtc.AddDays(1);

            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "EmployeeId", employeeIdFilter },
                    {
                        "Date", new BsonDocument
                        {
                            { "$gte", startUtc },
                            { "$lt", endUtc }
                        }
                    }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$EmployeeId" },
                    { "totalHours", new BsonDocument("$sum", "$Hours") }
                })
            };

            var aggregationResult = await _ts
                .Aggregate<BsonDocument>(pipeline)
                .ToListAsync(cancellationToken);

            double existingHours = 0.0;
            if (aggregationResult.Count > 0)
            {
                var val = aggregationResult[0]["totalHours"];
                if (val != BsonNull.Value)
                {
                    if (val is BsonDecimal128 d128)
                    {
                        existingHours = (double)d128.AsDecimal;
                    }
                    else
                    {
                        existingHours = val.ToDouble();
                    }
                }
            }

            double totalHours = existingHours + (double)newHours;
            bool isValid = totalHours <= MaxHoursPerDay;

            return new TimeEntryCheckResult
            {
                ExistingHours = existingHours,
                TotalHoursAfterAdd = totalHours,
                IsValid = isValid,
                ErrorMessage = isValid
                    ? null
                    : $"Превышен лимит часов. Уже учтено: {existingHours:F1} ч, добавляем: {newHours:F1} ч. Итого: {totalHours:F1} ч (максимум: {MaxHoursPerDay} ч)."
            };
        }
    }
}
