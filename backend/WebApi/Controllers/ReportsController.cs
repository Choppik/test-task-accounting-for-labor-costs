using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApi.DTO;
using WebApi.Models;
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
            if (string.IsNullOrWhiteSpace(request.Year) || string.IsNullOrWhiteSpace(request.Month))
            {
                return BadRequest(new { message = "Год и месяц обязательны" });
            }

            if (!int.TryParse(request.Year, out var year) || !int.TryParse(request.Month, out var month))
            {
                return BadRequest(new { message = "Год и месяц должны быть корректными числами" });
            }

            try
            {
                var query = new GetProjectsReportQuery
                {
                    Year = year,
                    Month = month,
                    SearchQuery = request.Q,
                    Page = request.Page,
                    PageSize = request.PageSize
                };

                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка при формировании отчёта", details = ex.Message });
            }
        }
    }
}
