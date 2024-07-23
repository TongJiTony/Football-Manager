using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
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

        // GET /v1/team//admin/displayall
        [HttpGet("admin/displayall")]
        public async Task<IActionResult> GetAllTeams([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string key = "")
        {
            try
            {
                int startRow = (page - 1) * limit + 1;
                int endRow = page * limit;

                string query = @"
        SELECT * FROM (
            SELECT 
                t.team_id, 
                t.team_name, 
                TO_CHAR(t.established_date,'YYYY-MM-DD') AS established_date, 
                t.head_coach, 
                t.city,
                ROW_NUMBER() OVER (ORDER BY t.team_name) AS rnum
            FROM 
                teams t
            WHERE 
                t.team_name LIKE '%' || :key || '%' 
                OR t.city LIKE '%' || :key2 || '%'
        ) 
        WHERE rnum BETWEEN :startRow AND :endRow";

                string countQuery = @"
        SELECT COUNT(*) AS total_count
        FROM teams t
        WHERE 
            t.team_name LIKE '%' || :key || '%' 
            OR t.city LIKE '%' || :key2 || '%'";

                var parameters = new Dictionary<string, object>
                {
                    { "key", key },
                    { "key2", key },
                    { "startRow", startRow },
                    { "endRow", endRow }
                };

                // Execute main query
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);

                // Execute count query
                var countParameters = new Dictionary<string, object>
                {
                    { "key", key },
                    { "key2", key }
                };
                List<Dictionary<string, object>> countResult = await _context.ExecuteQueryAsync(countQuery, countParameters);
                int totalCount = Convert.ToInt32(countResult[0]["TOTAL_COUNT"]);

                return Ok(new { data = result, total = totalCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // GET /v1/team/displayall or GET /v1/team/displayall?teamid=*
        [HttpGet("displayall")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT team_id,team_name, TO_CHAR(established_date,'YYYY-MM-DD') AS established_date, head_coach, city FROM teams ORDER BY team_name";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }
        [HttpGet("displayone")]
        public async Task<IActionResult> Get(string Teamid)
        {
            string query = "SELECT team_id,team_name, TO_CHAR(established_date,'YYYY-MM-DD') AS established_date, head_coach, city FROM teams WHERE team_id = :id";
            var parameters = new Dictionary<string, object> { { "id", Teamid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }
        [HttpPost("add")]// POST /v1/team/add
        public async Task<IActionResult> Post([FromBody] JsonElement teamElement)
        {
            string query = @"INSERT INTO teams (team_name, established_date, head_coach, city,team_id) VALUES (:name, :checkdate, :coach, :city,TEAM_SEQ.NEXTVAL) RETURNING team_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

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
            Console.WriteLine($"Generated Query: {query}");
            //await _context.ExecuteNonQueryAsync(query, parameters);
            //return CreatedAtAction(nameof(Get), new { Teamid = parameters["id"] }, parameters);

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newTeamid = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);
            return CreatedAtAction(nameof(Get), new { Teamid = newTeamid }, new { Teamid = newTeamid });
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string Teamid)
        {
            string query = "DELETE FROM teams WHERE team_id = :id";
            var parameters = new Dictionary<string, object>
    {
        { "id", Teamid }
    };

            try
            {
                int result = await _context.ExecuteNonQueryAsync(query, parameters);
                if (result > 0)
                {
                    return Ok(new { message = "Team deleted successfully", Teamid = Teamid });
                }
                else
                {
                    return NotFound(new { message = "Team not found", Teamid = Teamid });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("admin/delete")]
        public async Task<IActionResult> DeleteByIds([FromBody] int[] Teamids)
        {
            string query = "DELETE FROM teams WHERE team_id = :id";

            try
            {
                int totalDeleted = 0;

                foreach (var Teamid in Teamids)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "id", Teamid }
                    };

                    int result = await _context.ExecuteNonQueryAsync(query, parameters);

                    if (result > 0)
                    {
                        totalDeleted++;
                    }
                }

                if (totalDeleted > 0)
                {
                    return Ok(new { message = "Teams deleted successfully", deletedCount = totalDeleted });
                }
                else
                {
                    return NotFound(new { message = "No teams found for deletion" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> Post(string updateTeamid, [FromBody] JsonElement teamElement)
        {
            if (!teamElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE teams SET ");
            var parameters = new Dictionary<string, object>();
   

            // 第一个循环生成查询字符串
            foreach (var property in teamElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "team_name":
                        queryBuilder.Append("team_name = :team_name, ");
                        break;
                    case "established_date":
                        queryBuilder.Append("established_date = :established_date, ");
                        break;
                    case "head_coach":
                        queryBuilder.Append("head_coach = :head_coach, ");
                        break;
                    case "city":
                        queryBuilder.Append("city = :city, ");
                        break;
                    case "team_id":
                        queryBuilder.Append("team_id = :team_id, ");
                        break;
                    default:
                        break;
                }
            }

            // 移除最后一个逗号和空格
            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE team_id = :updateTeamid");
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            // 第二个循环添加参数
            foreach (var property in teamElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "team_name":
                        parameters.Add("team_name", property.Value.GetString());
                        break;
                    case "established_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("established_date", dateValue);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format for established_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for established_date.");
                        }
                        break;
                    case "head_coach":
                        parameters.Add("head_coach", property.Value.GetString());
                        break;
                    case "city":
                        parameters.Add("city", property.Value.GetString());
                        break;
                    case "team_id":
                        if (int.TryParse(property.Value.GetString(), out int teamId))
                        {
                            parameters.Add("team_id", teamId);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for team_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for team_id.");
                        }
                        break;
                    default:
                        break;
                }
            }
            parameters.Add("updateTeamid", updateTeamid);
            
            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing POST request: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
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
