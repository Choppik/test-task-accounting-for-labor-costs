using MediatR;
using System.Collections.Generic;
using WebApi.DTO;

namespace WebApi.Queries
{
    public class GetProjectsReportQuery : IRequest<List<ProjectReportItemDTO>>
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
