using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;
using WebApi.DTO;
using WebApi.Queries;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator, IMongoDatabase database)
        {
            _mediator = mediator;
        }

        [HttpPost("projects")]
        public async Task<IActionResult> GetProjectsReport([FromBody] ReportDTO request)
        {
            var query = new GetProjectsReportQuery
            {
                Year = int.Parse(request.Year),
                Month = int.Parse(request.Month),
                Page = request.Page,
                PageSize = request.PageSize
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
