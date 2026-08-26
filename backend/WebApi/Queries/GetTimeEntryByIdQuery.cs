using MediatR;
using WebApi.DTO;

namespace WebApi.Queries
{
    public class GetTimeEntryByIdQuery : IRequest<TimeEntryDTO>
    {
        public string Id { get; set; }
    }
}
