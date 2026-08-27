using MediatR;
using System.Collections.Generic;
using WebApi.Controllers;
using WebApi.DTO;

namespace WebApi.Queries
{

    public class GetProjectsReportQuery : IRequest<List<ProjectReportItem>>
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string? SearchQuery { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
