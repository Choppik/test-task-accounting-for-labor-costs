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
    public class GetTimeEntriesHandler : IRequestHandler<GetTimeEntriesQuery, GridResult<TimeEntryDTO>>
    {
        private readonly IMongoCollection<TimeEntry> _ts;

        public GetTimeEntriesHandler(IMongoDatabase database)
        {
            _ts = database.GetCollection<TimeEntry>("TimeEntries");
        }

        public async Task<GridResult<TimeEntryDTO>> Handle(GetTimeEntriesQuery request, CancellationToken cancellationToken)
        {
            var skip = (request.Page - 1) * request.PageSize;

            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$sort", new BsonDocument("date", -1)),
            
                new BsonDocument("$skip", skip),
            
                new BsonDocument("$limit", request.PageSize),
            
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Projects" }, 
                    { "localField", "ProjectId" }, 
                    { "foreignField", "_id" },    
                    { "as", "matchedProjects" }   
                }),

                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Employees" },
                    { "localField", "EmployeeId" },
                    { "foreignField", "_id" },
                    { "as", "matchedEmployees" }
                }),

                new BsonDocument("$addFields", new BsonDocument
                {
                    {
                        "resolvedProjectCode",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$matchedProjects.Code", 0 }),
                            "$projectId"
                        })
                    },
                    {
                        "resolvedEmployeeName",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$matchedEmployees.FullName", 0 }),
                            "$employeeId"
                        })
                    }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 1 },
                    { "ProjectId", 1 },
                    { "EmployeeId", 1 },
                    { "Hours", 1 },
                    { "Date", 1 },
                    { "Comment", 1 },
                    { "Version", 1 },
                    { "ExpectedCost", 1 },
                    { "projectCode", "$resolvedProjectCode" },
                    { "employeeFullName", "$resolvedEmployeeName" }
                }),
            };


            try
            {
                var result = await _ts.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken);

                Console.WriteLine($"[RESULT] Aggregation returned {result.Count} items.");

                var dtos = new List<TimeEntryDTO>();

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
                            try { return Convert.ToDecimal(val); } catch { return 0m; }
                        }

                        var dateRaw = r.GetValue("Date");
                        DateTime dateSafe = default;
                        if (dateRaw != null && !dateRaw.IsBsonNull && dateRaw is BsonDateTime dt)
                            dateSafe = dt.ToLocalTime();

                        decimal hoursSafe = GetDecimalSafe(r.GetValue("Hours"));
                        decimal expectedSafe = GetDecimalSafe(r.GetValue("ExpectedCost"));

                        int versionSafe = 0;
                        var vVal = r.GetValue("Version");
                        if (vVal is BsonInt32 vi) versionSafe = vi.AsInt32;
                        else if (vVal != null) versionSafe = Convert.ToInt32(vVal);

                        string commentSafe = r.GetValue("Comment")?.AsString ?? string.Empty;
                        string projectCode = r.GetValue("projectCode")?.AsString ?? "Неизвестно";
                        string employeeFullName = r.GetValue("employeeFullName")?.AsString ?? "Неизвестно";

                        dtos.Add(new TimeEntryDTO
                        {
                            Id = r["_id"].ToString(),
                            ProjectId = r["ProjectId"].ToString(),
                            EmployeeId = r["EmployeeId"].ToString(),
                            ExpectedCost = expectedSafe,
                            EmployeeFullName = employeeFullName,
                            ProjectCode = projectCode,
                            Date = dateSafe,
                            Hours = hoursSafe,
                            Comment = commentSafe,
                            Version = versionSafe
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Mapping error at row #{i}: {ex.Message}");
                        continue;
                    }
                }

                var totalCount = await _ts.CountDocumentsAsync(FilterDefinition<TimeEntry>.Empty, cancellationToken: cancellationToken);

                return new GridResult<TimeEntryDTO>
                {
                    Rows = dtos,
                    TotalRowCount = totalCount
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRITICAL] Aggregation failed: {ex.Message}");
                throw;
            }
        }
    }
}
