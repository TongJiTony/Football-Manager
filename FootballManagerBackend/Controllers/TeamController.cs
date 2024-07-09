using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FootballManagerBackend.Controllers
{
    [Route("v1/team")]
    [ApiController]
    public class TeamController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public TeamController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("{Teamid}")]
        public async Task<IActionResult> Get(string Teamid)
        {
            string query = "SELECT * FROM teams WHERE team_id = :id";
            var parameters = new Dictionary<string, object> { { "id", Teamid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }
        /*[HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<V1teamController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<V1teamController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<V1teamController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<V1teamController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }*/
    }
}
