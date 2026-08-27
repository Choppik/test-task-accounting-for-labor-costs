using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<ActionResult<List<TimeEntryDTO>>> GetAll()
        {
            var docs = await _ts.Find(Builders<TimeEntry>.Filter.Empty)
                                        .Limit(1000)
                                        .ToListAsync();

            var empIds = docs.Select(d => d.EmployeeId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            var projIds = docs.Select(d => d.ProjectId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

            var employees = empIds.Any()
                ? await _employees.Find(e => empIds.Contains(e.Id)).ToListAsync()
                : new List<Employee>();

            var projects = projIds.Any()
                ? await _projects.Find(p => projIds.Contains(p.Id)).ToListAsync()
                : new List<Project>();

            var empDict = employees.ToDictionary(e => e.Id, e => e.FullName);
            var projDict = projects.ToDictionary(p => p.Id, p => p.Code);

            var dtos = docs.Select(d =>
            {
                var dto = TimeEntryMapper.ToDTO(d);
                dto.EmployeeFullName = d.EmployeeId != null && empDict.TryGetValue(d.EmployeeId, out var en) ? en : d.EmployeeId;
                dto.ProjectCode = d.ProjectId != null && projDict.TryGetValue(d.ProjectId, out var pn) ? pn : d.ProjectId;
                return dto;
            }).ToList();

            return Ok(dtos);
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
