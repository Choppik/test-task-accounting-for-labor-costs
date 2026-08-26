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

        public GetTimeEntryByIdHandler(IMongoDatabase db)
        {
            _ts = db.GetCollection<TimeEntry>("TimeEntries");
        }

        public async Task<TimeEntryDTO> Handle(GetTimeEntryByIdQuery request, CancellationToken cancellationToken)
        {
            var e = await _ts.Find(x => x.Id == request.Id).FirstOrDefaultAsync(cancellationToken);
            if (e == null) return null;

            return new TimeEntryDTO
            {
                Id = e.Id,
                EmployeeId = e.EmployeeId,
                ProjectId = e.ProjectId,
                Date = e.Date,
                Hours = e.Hours,
                Comment = e.Comment,
                CreatedBy = e.CreatedBy,
                CreatedAt = e.CreatedAt,
                ModifiedBy = e.ModifiedBy,
                ModifiedAt = e.ModifiedAt,
                Version = e.Version
            };
        }
    }
}