using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FootballManagerBackend.Controllers
{
    [Route("v1/stadium")]
    [ApiController]
    public class StadiumController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public StadiumController(OracleDbContext context)
        {
            _context = context;
        }
        // GET /v1/stadium/displayall or GET /v1/stadium/displayall?stadiumid=*
        [HttpGet("displayall")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT * FROM stadiums ORDER BY stadium_name";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }
        [HttpGet("displayone")]
        public async Task<IActionResult> Get(string Stadiumid)
        {
            string query = "SELECT * FROM stadiums WHERE stadium_id = :id";
            var parameters = new Dictionary<string, object> { { "id", Stadiumid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }








    }
}
