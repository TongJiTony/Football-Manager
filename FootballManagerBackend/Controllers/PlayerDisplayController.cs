using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("v1/playerdisplay")]
    public class DataController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public DataController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            string query = "SELECT * FROM players WHERE player_id='" + id + "'";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }
    }
}
