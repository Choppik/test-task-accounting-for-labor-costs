using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebApi.DTO;
using WebApi.Models;
using WebApi.Queries;

namespace WebApi.Handlers
{
    public class GetProjectsReportHandler : IRequestHandler<GetProjectsReportQuery, List<ProjectReportItemDTO>>
    {
        private readonly IMongoCollection<Project> _projects;

        public GetProjectsReportHandler(IMongoDatabase database)
        {
            _projects = database.GetCollection<Project>("Projects");
        }

        public async Task<List<ProjectReportItemDTO>> Handle(GetProjectsReportQuery request, CancellationToken cancellationToken)
        {
            var startOfMonth = new DateTime(request.Year, request.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1).AddSeconds(-1);

            Console.WriteLine($"[DEBUG] Month range: {startOfMonth:yyyy-MM-dd} — {endOfMonth:yyyy-MM-dd}");

            var matchFilter = new BsonDocument
            {
                { "StartDate", new BsonDocument("$lte", endOfMonth) }
            };

            var endDateConditions = new BsonArray
            {
                new BsonDocument("EndDate", new BsonDocument("$gte", startOfMonth)),
                new BsonDocument("EndDate", BsonNull.Value)
            };
            matchFilter.Add("$or", endDateConditions);

            var skip = (request.Page - 1) * request.PageSize;

            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", matchFilter),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "TimeEntries" },
                    { "let", new BsonDocument { { "projectObjId", "$_id" } } },
                    {
                        "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument
                            {
                                {
                                    "$expr", new BsonDocument
                                    {
                                        {
                                            "$eq", new BsonArray
                                            {
                                                new BsonDocument("$toObjectId", "$ProjectId"),
                                                "$$projectObjId"
                                            }
                                        }
                                    }
                                }
                            })
                        }
                    },
                    { "as", "TimeEntries" }
                }),

                new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", "$TimeEntries" },
                    { "preserveNullAndEmptyArrays", false }
                }),

                new BsonDocument("$match", new BsonDocument
                {
                    {
                        "TimeEntries.Date", new BsonDocument
                        {
                            { "$gte", startOfMonth },
                            { "$lte", endOfMonth }
                        }
                    }
                }),


                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$_id" },
                    { "code", new BsonDocument("$first", "$Code") },
                    { "name", new BsonDocument("$first", "$Name") },
                    { "budgetRub", new BsonDocument("$first", "$BudgetRub") },
                    { "startDate", new BsonDocument("$first", "$StartDate") },
                    { "endDate", new BsonDocument("$first", "$EndDate") },
                    {
                        "totalHours",
                        new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$TimeEntries.Hours", 0 }))
                    },
                    {
                        "totalCost",
                        new BsonDocument("$sum", new BsonDocument("$ifNull", new BsonArray { "$TimeEntries.ExpectedCost", 0 }))
                    }
                }),

                new BsonDocument("$addFields", new BsonDocument
                {
                    {
                        "spentPercent",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { "$budgetRub", 0 }),
                            0,
                            new BsonDocument("$multiply", new BsonArray
                            {
                                new BsonDocument("$divide", new BsonArray { "$totalCost", "$budgetRub" }),
                                100
                            })
                        })
                    },
                    {
                        "budgetNote",
                        new BsonDocument("$cond", new BsonArray
                        {
                            new BsonDocument("$gt", new BsonArray { "$totalCost", "$budgetRub" }),
                            "Перерасход бюджета за месяц",
                            "В рамках бюджета"
                        })
                    }
                }),

                new BsonDocument("$skip", skip),
                new BsonDocument("$limit", request.PageSize)
            };

            var result = await _projects.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);
            Console.WriteLine($"[RESULT] Aggregation returned {result.Count} items.");

            var reportItems = new List<ProjectReportItemDTO>();

            for (int i = 0; i < result.Count; i++)
            {
                var r = result[i];
                try
                {
                    decimal GetDecimalSafe(BsonValue val)
                    {
                        if (val == null || val.IsBsonNull) return 0m;
                        if (val is BsonDecimal128 d128) return d128.AsDecimal;
                        if (val is BsonInt32 i32) return i32.AsInt32;
                        if (val is BsonDouble dbl) return (decimal)dbl;
                        return Convert.ToDecimal(val);
                    }

                    var endDateRaw = r.GetValue("endDate");
                    DateTime? endDateSafe = null;
                    if (endDateRaw != null && !endDateRaw.IsBsonNull && endDateRaw is BsonDateTime endDt)
                        endDateSafe = endDt.ToLocalTime();

                    var startDateRaw = r.GetValue("startDate");
                    DateTime startDateSafe;
                    if (startDateRaw != null && !startDateRaw.IsBsonNull && startDateRaw is BsonDateTime startDt)
                        startDateSafe = startDt.ToLocalTime();
                    else
                        startDateSafe = DateTime.UtcNow.ToLocalTime();

                    decimal budgetRub = GetDecimalSafe(r.GetValue("budgetRub"));
                    decimal totalCost = GetDecimalSafe(r.GetValue("totalCost"));
                    decimal spentPercent = GetDecimalSafe(r.GetValue("spentPercent"));

                    int totalHours = 0;
                    var hoursVal = r.GetValue("totalHours");
                    if (hoursVal != null && !hoursVal.IsBsonNull)
                    {
                        if (hoursVal is BsonInt32 i32) totalHours = i32.AsInt32;
                        else totalHours = Convert.ToInt32(hoursVal);
                    }

                    reportItems.Add(new ProjectReportItemDTO
                    {
                        Id = r["_id"].ToString(),
                        ProjectCode = r.GetValue("code")?.AsString ?? string.Empty,
                        ProjectName = r.GetValue("name")?.AsString ?? string.Empty,
                        BudgetRub = budgetRub,
                        TotalHours = totalHours,
                        TotalCost = totalCost,
                        PercentSpent = spentPercent,    
                        StartDate = startDateSafe,
                        EndDate = endDateSafe
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to map row #{i}: {ex.Message}");
                    continue;
                }
            }

            return reportItems;
        }
    }
}