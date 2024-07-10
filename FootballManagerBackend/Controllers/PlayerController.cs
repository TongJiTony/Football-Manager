using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("v1/player")]
    public class DataController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public DataController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT player_id, player_name, team_id FROM players";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            string query = "SELECT * FROM players WHERE player_id = :id";
            var parameters = new Dictionary<string, object> { { "id", id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement teamElement)
        {
            string query = "INSERT INTO teams (team_name, established_date, head_coach, city, team_id) VALUES (:name, :checkdate, :coach, :city, :id)";

            var parameters = new Dictionary<string, object>();

            // 从 JsonElement 中获取值，并进行类型转换
            foreach (var property in teamElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "team_name":
                        parameters.Add("name", property.Value.GetString());
                        break;
                    case "established_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("checkdate", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for established_date: {property.Value.GetString()}");

                        }
                        break;
                    case "head_coach":
                        parameters.Add("coach", property.Value.GetString());
                        break;
                    case "city":
                        parameters.Add("city", property.Value.GetString());
                        break;
                    case "team_id":
                        parameters.Add("id", property.Value.GetInt32());
                        break;
                    default:

                        break;
                }
            }

            await _context.ExecuteNonQueryAsync(query, parameters);
            return CreatedAtAction(nameof(Get), new { Teamid = parameters["id"] }, parameters);
        }
    }

    
}
