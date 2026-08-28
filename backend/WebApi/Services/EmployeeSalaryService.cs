using System;
using System.Collections.Generic;
using System.Linq;
using WebApi.Models;

namespace WebApi.Services
{
    public class EmployeeSalaryService
    {
        public static decimal GetActualyRateAt(IEnumerable<SalaryHistory> history, DateTime at)
        {
            if (history == null) return 0m;

            var utcAt = at.Kind == DateTimeKind.Utc ? at : at.ToUniversalTime();

            var entry = history
                .Where(s => s.EffectiveFrom.Kind == DateTimeKind.Utc ? s.EffectiveFrom <= utcAt : s.EffectiveFrom.ToUniversalTime() <= utcAt)
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefault();

            if (entry != null) return entry.HourlyRate;

            var first = history.OrderBy(s => s.EffectiveFrom).FirstOrDefault();
            if (first != null) return first.HourlyRate;

            return 0m;
        }

        public static decimal GetRateAt(IEnumerable<SalaryHistory> history, DateTime at)
        {
            if (history == null || !history.Any())
                return 0m;

            var utcAt = at.Kind == DateTimeKind.Utc ? at : at.ToUniversalTime();

            var activeRate = history
                .Select(s => new
                {
                    Rate = s.HourlyRate,
                    Start = s.EffectiveFrom.Kind == DateTimeKind.Utc
                        ? s.EffectiveFrom
                        : s.EffectiveFrom.ToUniversalTime()
                })
                // Оставляем только ставки, которые начались ДО или В день запроса
                .Where(s => s.Start <= utcAt)
                // Сортируем по дате начала: от новых к старым
                .OrderByDescending(s => s.Start)
                // Берём первую (самую свежую из подходящих)
                .FirstOrDefault();

            return activeRate?.Rate ?? 0m;
        }
    }
}
