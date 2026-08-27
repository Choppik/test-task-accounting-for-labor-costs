using MediatR;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Models;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IMongoCollection<Project> _projects;

        public ProjectsController(IMongoDatabase database)
        {
            _projects = database.GetCollection<Project>("Projects");
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q = "", int page = 1, int pageSize = 20)
        {
            var filter = string.IsNullOrWhiteSpace(q)
                ? Builders<Project>.Filter.Empty
                : Builders<Project>.Filter.Or(
                    Builders<Project>.Filter.Regex(e => e.Code, new MongoDB.Bson.BsonRegularExpression(q, "i"))
                  );

            var skip = (page - 1) * pageSize;
            var list = await _projects.Find(filter)
                                 .Project(e => new { id = e.Id, code = e.Code })
                                 .Skip(skip)
                                 .Limit(pageSize)
                                 .ToListAsync();

            return Ok(list);
        }

        // GET /api/projects/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var e = await _projects.Find(x => x.Id == id)
                              .Project(x => new { id = x.Id, code = x.Code })
                              .FirstOrDefaultAsync();
            if (e == null) return NotFound();
            return Ok(e);
        }
    }
}
