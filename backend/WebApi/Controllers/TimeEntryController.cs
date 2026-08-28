using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Commands;
using WebApi.DTO;
using WebApi.Models;
using WebApi.Queries;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private readonly IMongoCollection<TimeEntry> _ts;
        private readonly IMongoCollection<Employee> _employees;
        private readonly IMongoCollection<Project> _projects;
        private readonly IMediator _mediator;

        public TimeEntryController(IMediator mediator, IMongoDatabase database)
        {
            _ts = database.GetCollection<TimeEntry>("TimeEntries");
            _employees = database.GetCollection<Employee>("Employees");
            _projects = database.GetCollection<Project>("Projects");
            _mediator = mediator;
        }

        // GET api/timeentry
        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? page = 1,
            [FromQuery] int? pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            // 1. Безопасное приведение типов с дефолтными значениями
            var safePage = page.HasValue && page.Value > 0 ? page.Value : 1;
            var safePageSize = pageSize.HasValue && pageSize.Value > 0 && pageSize.Value <= 100
                ? pageSize.Value
                : 20; // Максимум 100 записей за запрос — защита от перегрузки БД

            var query = new GetTimeEntriesQuery
            {
                Page = safePage,
                PageSize = safePageSize
            };

            try
            {
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result.Rows);
            }
            catch (Exception ex)
            {
                // В реальном проекте лучше логировать ex и возвращать более понятный код ошибки
                return StatusCode(500, $"Ошибка при получении списка табелей: {ex.Message}");
            }
        }

        // GET api/timeentry/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeEntryDTO>> GetById(string id)
        {
            var dto = await _mediator.Send(new GetTimeEntryByIdQuery { Id = id });
            if (dto != null) return Ok(dto);

            return NotFound();
        }

        // POST api/timeentry
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTimeEntryCommand cmd)
        {
            var id = await _mediator.Send(cmd);

            var created = await _ts.Find(e => e.Id == id).FirstOrDefaultAsync();
            if (created == null) return CreatedAtAction(nameof(GetById), new { id }, new { id });

            var dto = TimeEntryMapper.ToDTO(created);
            return CreatedAtAction(nameof(GetById), new { id }, dto);
        }

        // PUT api/timeentry/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTimeEntryCommand cmd)
        {
            cmd.Id = id;
            var ok = await _mediator.Send(cmd);
            if (!ok) return NotFound();

            var updated = await _ts.Find(e => e.Id == id).FirstOrDefaultAsync();
            if (updated == null) return NoContent();

            var dto = TimeEntryMapper.ToDTO(updated);
            return Ok(dto);
        }

        // DELETE api/timeentry/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteTimeEntryCommand { Id = id });
            if (!ok) return NotFound();
            return NoContent();
        }
    }
}
