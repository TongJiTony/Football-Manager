using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("v1/lineup")]
    public class LineupController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public LineupController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/lineup/displayall or GET /v1/lineup/displayall?teamid=*
        public async Task<IActionResult> Get(int? teamid = null)
        {
            string query = @"SELECT lineup_id, note, team_id, team_name, match_id, 
                player1_id, player2_id, player3_id, player4_id, player5_id, player6_id, 
                player7_id, player8_id, player9_id, player10_id, player11_id 
                FROM lineups natural join teams ORDER BY lineup_id";
            if (teamid != null)
            {
                query = @"SELECT lineup_id, note, team_id, team_name, match_id, 
                player1_id, player2_id, player3_id, player4_id, player5_id, player6_id, 
                player7_id, player8_id, player9_id, player10_id, player11_id 
                FROM lineups natural join teams WHERE team_id = :teamid ORDER BY lineup_id";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("displayone")] // GET /v1/lineup/displayone?lineupid=*
        public async Task<IActionResult> Get(int lineupid)
        {
            string query = @"SELECT lineup_id, note, team_id, team_name, match_id, 
                player1_id, player2_id, player3_id, player4_id, player5_id, player6_id, 
                player7_id, player8_id, player9_id, player10_id, player11_id 
                FROM lineups natural join teams WHERE lineup_id = :Lineupid";
            var parameters = new Dictionary<string, object> { { "Lineupid", lineupid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/lineup/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement lineupElement)
        {
            string query = @"
            INSERT INTO lineups 
            (lineup_id, note, team_id, match_id, 
             player1_id, player2_id, player3_id, player4_id, player5_id, player6_id, 
             player7_id, player8_id, player9_id, player10_id, player11_id) 
            VALUES 
            (LINEUP_SEQ.NEXTVAL, :note, :team_id, :match_id, 
             :player1_id, :player2_id, :player3_id, :player4_id, :player5_id, :player6_id, 
             :player7_id, :player8_id, :player9_id, :player10_id, :player11_id) 
            RETURNING lineup_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in lineupElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "lineup_id":
                        parameters.Add("lineup_id", property.Value.GetInt32());
                        break;
                    case "note":
                        parameters.Add("note", property.Value.GetString());
                        break;
                    case "team_id":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "match_id":
                        parameters.Add("match_id", property.Value.GetInt32());
                        break;
                    case "player1_id":
                        parameters.Add("player1_id", property.Value.GetInt32());
                        break;
                    case "player2_id":
                        parameters.Add("player2_id", property.Value.GetInt32());
                        break;
                    case "player3_id":
                        parameters.Add("player3_id", property.Value.GetInt32());
                        break;
                    case "player4_id":
                        parameters.Add("player4_id", property.Value.GetInt32());
                        break;
                    case "player5_id":
                        parameters.Add("player5_id", property.Value.GetInt32());
                        break;
                    case "player6_id":
                        parameters.Add("player6_id", property.Value.GetInt32());
                        break;
                    case "player7_id":
                        parameters.Add("player7_id", property.Value.GetInt32());
                        break;
                    case "player8_id":
                        parameters.Add("player8_id", property.Value.GetInt32());
                        break;
                    case "player9_id":
                        parameters.Add("player9_id", property.Value.GetInt32());
                        break;
                    case "player10_id":
                        parameters.Add("player10_id", property.Value.GetInt32());
                        break;
                    case "player11_id":
                        parameters.Add("player11_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newLineupId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(Get), new { LINEUP_ID = newLineupId }, new { LINEUP_ID = newLineupId });
        }

        [HttpPost("update")] // POST /v1/lineup/update?lineupid=* + JSON
        public async Task<IActionResult> Update(int lineupid, [FromBody] JsonElement lineupElement)
        {
            if (!lineupElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE lineups SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in lineupElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "lineup_id":
                        if (int.TryParse(property.Value.GetString(), out int lineup_id))
                        {
                            queryBuilder.Append("lineup_id = :lineup_id, ");
                            parameters.Add("lineup_id", lineup_id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for lineup_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for lineup_id.");
                        }
                        break;
                    case "note":
                        queryBuilder.Append("note = :note, ");
                        parameters.Add("note", property.Value.GetString());
                        break;
                    case "team_id":
                        queryBuilder.Append("team_id = :team_id, ");
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "player1_id":
                        queryBuilder.Append("player1_id = :player1_id, ");
                        parameters.Add("player1_id", property.Value.GetInt32());
                        break;
                    case "player2_id":
                        queryBuilder.Append("player2_id = :player2_id, ");
                        parameters.Add("player2_id", property.Value.GetInt32());
                        break;
                    case "player3_id":
                        queryBuilder.Append("player3_id = :player3_id, ");
                        parameters.Add("player3_id", property.Value.GetInt32());
                        break;
                    case "player4_id":
                        queryBuilder.Append("player4_id = :player4_id, ");
                        parameters.Add("player4_id", property.Value.GetInt32());
                        break;
                    case "player5_id":
                        queryBuilder.Append("player5_id = :player5_id, ");
                        parameters.Add("player5_id", property.Value.GetInt32());
                        break;
                    case "player6_id":
                        queryBuilder.Append("player6_id = :player6_id, ");
                        parameters.Add("player6_id", property.Value.GetInt32());
                        break;
                    case "player7_id":
                        queryBuilder.Append("player7_id = :player7_id, ");
                        parameters.Add("player7_id", property.Value.GetInt32());
                        break;
                    case "player8_id":
                        queryBuilder.Append("player8_id = :player8_id, ");
                        parameters.Add("player8_id", property.Value.GetInt32());
                        break;
                    case "player9_id":
                        queryBuilder.Append("player9_id = :player9_id, ");
                        parameters.Add("player9_id", property.Value.GetInt32());
                        break;
                    case "player10_id":
                        queryBuilder.Append("player10_id = :player10_id, ");
                        parameters.Add("player10_id", property.Value.GetInt32());
                        break;
                    case "player11_id":
                        queryBuilder.Append("player11_id = :player11_id, ");
                        parameters.Add("player11_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE lineup_id = :lineupid");
            parameters.Add("lineupid", lineupid);
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

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

        [HttpDelete("delete")] // DELETE /v1/lineup/delete?lineupid=*
        public async Task<IActionResult> Delete(int lineupid)
        {
            string query = "DELETE FROM lineups WHERE lineupid = :lineupid";
            var parameters = new Dictionary<string, object> { { "lineupid", lineupid } };

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
