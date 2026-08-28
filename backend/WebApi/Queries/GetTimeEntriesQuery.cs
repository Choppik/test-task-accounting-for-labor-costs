using MediatR;
using WebApi.DTO;

namespace WebApi.Queries
{
    public class GetTimeEntriesQuery : IRequest<GridResult<TimeEntryDTO>>
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
    }
}
