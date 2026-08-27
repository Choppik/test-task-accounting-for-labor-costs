using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Driver;
using WebApi.DTO;
using WebApi.Models;
using WebApi.Queries;

namespace WebApi.Handlers.Timesheets
{
    public class GetTimeEntryByIdHandler : IRequestHandler<GetTimeEntryByIdQuery, TimeEntryDTO>
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<Project> _projects;

        public GetTimeEntryByIdHandler(IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
            _employees = db.GetCollection<Employee>("Employees");
            _projects = db.GetCollection<Project>("Projects");
        }

        public async Task<TimeEntryDTO> Handle(GetTimeEntryByIdQuery request, CancellationToken cancellationToken)
        {
            var ts = await _ts.Find(x => x.Id == request.Id).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (ts == null) return null;

            var emp = _employees.Find(e => e.Id == ts.EmployeeId).FirstOrDefaultAsync(cancellationToken: cancellationToken);
            var proj = _projects.Find(g => g.Id == ts.ProjectId).FirstOrDefaultAsync(cancellationToken: cancellationToken);

            return new TimeEntryDTO
            {
                Id = ts.Id,
                EmployeeId = ts.EmployeeId,
                ProjectId = ts.ProjectId,
                Date = ts.Date,
                Hours = ts.Hours,
                ExpectedCost = ts.ExpectedCost,
                EmployeeFullName = emp.Result.FullName,
                ProjectCode = proj.Result.Code,
                Comment = ts.Comment,
                CreatedBy = ts.CreatedBy,
                CreatedAt = ts.CreatedAt,
                ModifiedBy = ts.ModifiedBy,
                ModifiedAt = ts.ModifiedAt,
                Version = ts.Version
            };
        }
    }
}