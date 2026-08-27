using System;
using System.Collections.Generic;
using System.Linq;
using WebApi.Models;

namespace WebApi.Services
{
    public class EmployeeSalaryService
    {
        public static decimal GetRateAt(IEnumerable<SalaryHistory> history, DateTime at)
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
    }
}
