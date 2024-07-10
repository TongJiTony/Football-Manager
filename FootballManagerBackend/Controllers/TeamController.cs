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
        [HttpGet("all")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT * FROM teams";
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
