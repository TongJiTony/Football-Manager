using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        // GET /v1/team/displayall or GET /v1/team/displayall?teamid=*
        [HttpGet("displayall")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT * FROM teams ORDER BY team_name";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }
        [HttpGet("{Teamid}")]
        public async Task<IActionResult> Get(string Teamid)
        {
            string query = "SELECT * FROM teams WHERE team_id = :id";
            var parameters = new Dictionary<string, object> { { "id", Teamid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }
        [HttpPost("add")]// POST /v1/player/add
        public async Task<IActionResult> Post([FromBody] JsonElement teamElement)
        {
            string query = "INSERT INTO teams (team_name, established_date, head_coach, city) VALUES (:name, :checkdate, :coach, :city)";

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
/*
        [HttpPost("{updateTeamid}")]
        public async Task<IActionResult> POST(int updateTeamid, [FromBody] JsonElement teamElement)
        {
            if (!teamElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE teams SET ");
            var parameters = new Dictionary<string, object>
    {
        { "updateTeamid", updateTeamid } // 预设ID参数，用于WHERE子句
    };

            foreach (var property in teamElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "team_name":
                        queryBuilder.Append("team_name = :name, ");
                        parameters.Add("name", property.Value.GetString());
                        break;
                    case "established_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            queryBuilder.Append("established_date = :checkdate, ");
                            parameters.Add("checkdate", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for established_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for established_date.");
                        }
                        break;
                    case "head_coach":
                        queryBuilder.Append("head_coach = :coach, ");
                        parameters.Add("coach", property.Value.GetString());
                        break;
                    case "city":
                        queryBuilder.Append("city = :city, ");
                        parameters.Add("city", property.Value.GetString());
                        break;
                    case "team_id":
                        if (int.TryParse(property.Value.GetString(), out int teamId))
                        {
                            queryBuilder.Append("team_id = :team_id, ");
                            parameters.Add("team_id", teamId);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for team_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for team_id.");
                        }
                        break;
                    default:
                        // 忽略未知字段
                        break;
                }
            }

            // 移除最后一个逗号和空格
            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE team_id = :updateTeamid");
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing PUT request: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
        */


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
