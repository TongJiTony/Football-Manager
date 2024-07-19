using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text.Json;

namespace FootballManagerBackend.Controllers
{
    [Route("v1/match")]
    [ApiController]
    public class MatchController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public MatchController(OracleDbContext context)
        {
            _context = context;
        }

        // GET /v1/match/displayall
        [HttpGet("displayall")]
        public async Task<IActionResult> Get()
        {
            string query = @"
        SELECT 
            m.match_id, 
            TO_CHAR(m.match_date, 'YYYY-MM-DD') AS match_date, 
            s.stadium_name AS match_stadium, 
            ht.team_name AS home_team_name, 
            at.team_name AS away_team_name, 
            m.home_team_score, 
            m.away_team_score,
            m.home_team_id,
            m.away_team_id
        FROM 
            matches m
        JOIN 
            stadiums s ON m.match_stadium = s.stadium_id
        JOIN 
            teams ht ON m.home_team_id = ht.team_id
        JOIN 
            teams at ON m.away_team_id = at.team_id
        ORDER BY 
            m.match_date";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }


        [HttpGet("search")]
        public async Task<IActionResult> Search(
         [FromQuery] string? match_id = null,
         [FromQuery] string? match_date = null,
         [FromQuery] string? match_stadium = null,
         [FromQuery] string? home_team_id = null,
         [FromQuery] string? away_team_id = null)
        {
            var queryBuilder = new System.Text.StringBuilder(@"
        SELECT 
            m.match_id, 
            TO_CHAR(m.match_date, 'YYYY-MM-DD') AS match_date, 
            s.stadium_name AS match_stadium, 
            ht.team_name AS home_team_name, 
            at.team_name AS away_team_name, 
            m.home_team_score, 
            m.away_team_score,
            m.home_team_id,
            m.away_team_id
        FROM 
            matches m
        JOIN 
            stadiums s ON m.match_stadium = s.stadium_id
        JOIN 
            teams ht ON m.home_team_id = ht.team_id
        JOIN 
            teams at ON m.away_team_id = at.team_id
        WHERE 
            1=1");

            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(match_id))
            {
                queryBuilder.Append(" AND m.match_id = :match_id");
                parameters.Add("match_id", match_id);
            }

            if (!string.IsNullOrEmpty(match_date))
            {
                queryBuilder.Append(" AND TO_CHAR(m.match_date, 'YYYY-MM-DD') = :match_date");
                parameters.Add("match_date", match_date);
            }

            if (!string.IsNullOrEmpty(match_stadium))
            {
                queryBuilder.Append(" AND s.stadium_name = :match_stadium");
                parameters.Add("match_stadium", match_stadium);
            }

            if (!string.IsNullOrEmpty(home_team_id))
            {
                queryBuilder.Append(" AND m.home_team_id = :home_team_id");
                parameters.Add("home_team_id", home_team_id);
            }

            if (!string.IsNullOrEmpty(away_team_id))
            {
                queryBuilder.Append(" AND m.away_team_id = :away_team_id");
                parameters.Add("away_team_id", away_team_id);
            }

            string query = queryBuilder.ToString();
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }


        [HttpPost("add")]
        public async Task<IActionResult> Post([FromBody] JsonElement matchElement)
        {
            string query = @"INSERT INTO matches (match_date, match_stadium, home_team_id, away_team_id, home_team_score, away_team_score, match_id) VALUES (:matchdate, :stadium, :home_team, :away_team, :home_score, :away_score, MATCH_SEQ.NEXTVAL) RETURNING match_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in matchElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "match_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("matchdate", dateValue);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format for match_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for match_date.");
                        }
                        break;
                    case "match_stadium":
                        parameters.Add("stadium", property.Value.GetInt32());
                        break;
                    case "home_team_id":
                        parameters.Add("home_team", property.Value.GetInt32());
                        break;
                    case "away_team_id":
                        parameters.Add("away_team", property.Value.GetInt32());
                        break;
                    case "home_team_score":
                        parameters.Add("home_score", property.Value.GetInt32());
                        break;
                    case "away_team_score":
                        parameters.Add("away_score", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }
            Console.WriteLine($"Generated Query: {query}");

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newMatchId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);
            return CreatedAtAction(nameof(Get), new { matchid = newMatchId }, new { matchid = newMatchId });
        }

        // DELETE /v1/match/delete?matchid=*
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string matchid)
        {
            string query = "DELETE FROM matches WHERE match_id = :id";
            var parameters = new Dictionary<string, object> { { "id", matchid } };

            try
            {
                int result = await _context.ExecuteNonQueryAsync(query, parameters);
                if (result > 0)
                {
                    return Ok(new { message = "Match deleted successfully", matchid = matchid });
                }
                else
                {
                    return NotFound(new { message = "Match not found", matchid = matchid });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /v1/match/update
        [HttpPost("update")]
        public async Task<IActionResult> Update(string matchid, [FromBody] JsonElement matchElement)
        {
            if (!matchElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE matches SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in matchElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "match_date":
                        queryBuilder.Append("match_date = :date, ");
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("date", dateValue);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format for match_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for match_date.");
                        }
                        break;
                    case "match_stadium":
                        queryBuilder.Append("match_stadium = :stadium, ");
                        parameters.Add("stadium", property.Value.GetInt32());
                        break;
                    case "home_team_id":
                        queryBuilder.Append("home_team_id = :home_team, ");
                        parameters.Add("home_team", property.Value.GetInt32());
                        break;
                    case "away_team_id":
                        queryBuilder.Append("away_team_id = :away_team, ");
                        parameters.Add("away_team", property.Value.GetInt32());
                        break;
                    case "home_team_score":
                        queryBuilder.Append("home_team_score = :home_score, ");
                        parameters.Add("home_score", property.Value.GetInt32());
                        break;
                    case "away_team_score":
                        queryBuilder.Append("away_team_score = :away_score, ");
                        parameters.Add("away_score", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE match_id = :matchid");
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            parameters.Add("matchid", matchid);

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
    }
}
