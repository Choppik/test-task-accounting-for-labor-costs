using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IMongoCollection<Models.Project> _col;
        public ProjectsController(IMongoDatabase db) => _col = db.GetCollection<Models.Project>("Projects");

        // GET /api/projects?q=П-001&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q = "", int page = 1, int pageSize = 20)
        {
            var filter = string.IsNullOrWhiteSpace(q)
                ? Builders<Models.Project>.Filter.Empty
                : Builders<Models.Project>.Filter.Or(
                    Builders<Models.Project>.Filter.Regex(e => e.Code, new MongoDB.Bson.BsonRegularExpression(q, "i"))
                  );

            var skip = (page - 1) * pageSize;
            var list = await _col.Find(filter)
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
            var e = await _col.Find(x => x.Id == id)
                              .Project(x => new { id = x.Id, code = x.Code })
                              .FirstOrDefaultAsync();
            if (e == null) return NotFound();
            return Ok(e);
        }
    }
}
