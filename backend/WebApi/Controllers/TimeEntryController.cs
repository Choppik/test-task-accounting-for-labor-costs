using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApi.Commands;
using WebApi.Models;
using WebApi.Queries;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeEntryController : ControllerBase
    {
        private readonly IMongoCollection<TimeEntry> _collection;
        private readonly IMediator _mediator;

        public TimeEntryController(IMediator mediator, IMongoDatabase database)
        {
            _collection = database.GetCollection<TimeEntry>("TimeEntries");
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<ActionResult<List<TimeEntry>>> GetAll()
        {
            var list = await _collection.Find(Builders<TimeEntry>.Filter.Empty)
                                        .Limit(1000)
                                        .ToListAsync();
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTimeEntryCommand cmd)
        {
            var id = await _mediator.Send(cmd);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateTimeEntryCommand cmd)
        {
            cmd.Id = id;
            var ok = await _mediator.Send(cmd);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _mediator.Send(new DeleteTimeEntryCommand { Id = id });
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var dto = await _mediator.Send(new GetTimeEntryByIdQuery { Id = id });
            if (dto == null) return NotFound();
            return Ok(dto);
        }
    }
}
