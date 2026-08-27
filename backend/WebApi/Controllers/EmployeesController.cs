using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IMongoCollection<Models.Employee> _col;
        public EmployeesController(IMongoDatabase db) => _col = db.GetCollection<Models.Employee>("Employees");

        // GET /api/employees?q=иван&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q = "", int page = 1, int pageSize = 20)
        {
            var filter = string.IsNullOrWhiteSpace(q)
                ? Builders<Models.Employee>.Filter.Empty
                : Builders<Models.Employee>.Filter.Or(
                    Builders<Models.Employee>.Filter.Regex(e => e.FullName, new MongoDB.Bson.BsonRegularExpression(q, "i"))
                  );

            var skip = (page - 1) * pageSize;
            var list = await _col.Find(filter)
                                 .Project(e => new { id = e.Id, fullName = e.FullName})
                                 .Skip(skip)
                                 .Limit(pageSize)
                                 .ToListAsync();

            return Ok(list);
        }

        // GET /api/employees/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var e = await _col.Find(x => x.Id == id)
                              .Project(x => new { id = x.Id, fullName = x.FullName })
                              .FirstOrDefaultAsync();
            if (e == null) return NotFound();
            return Ok(e);
        }
    }
}