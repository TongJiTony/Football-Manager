using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace FootballManagerBackend.Controllers
{
    [ApiController]
    [Route("v1/player")]
    public class PlayerController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public PlayerController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/player/displayall or GET /v1/player/displayall?teamid=*
        public async Task<IActionResult> Get(string? teamid = null)
        {
            string query = "SELECT * FROM players ORDER BY player_name";
            if (teamid != null)
            {
                query = "SELECT * FROM players WHERE team_id = :teamid ORDER BY player_name";
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

        [HttpGet("displayone")] // GET /v1/player/displayone?playerid=*
        public async Task<IActionResult> Get(int playerid)
        {
            string query = "SELECT * FROM players WHERE player_id = :Playerid";
            var parameters = new Dictionary<string, object> { { "Playerid", playerid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/player/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement playerElement)
        {
            string query = "INSERT INTO players (player_id, player_name, birthday, team_id, role, used_foot, health_state, rank, game_state, trans_state, is_show) VALUES (:player_id, :player_name, :birthday, :team_id, :role, :used_foot, :health_state, :rank, :game_state, :trans_state, :is_show)";

            var parameters = new Dictionary<string, object>();

            foreach (var property in playerElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "player_name":
                        parameters.Add("player_name", property.Value.GetString());
                        break;
                    case "birthday":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("birthday", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for birthday: {property.Value.GetString()}");
                        }
                        break;
                    case "team_id":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "role":
                        parameters.Add("role", property.Value.GetString());
                        break;
                    case "used_foot":
                        parameters.Add("used_foot", property.Value.GetInt32());
                        break;
                    case "health_state":
                        parameters.Add("health_state", property.Value.GetInt32());
                        break;
                    case "rank":
                        parameters.Add("rank", property.Value.GetInt32());
                        break;
                    case "game_state":
                        parameters.Add("game_state", property.Value.GetInt32());
                        break;
                    case "trans_state":
                        parameters.Add("trans_state", property.Value.GetInt32());
                        break;
                    case "is_show":
                        parameters.Add("is_show", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsync(query, parameters);
            return CreatedAtAction(nameof(Get), new { Playerid = parameters["id"] }, parameters);
        }

        [HttpPost("update")] // POST /v1/player/update?playerid=* + JSON
        public async Task<IActionResult> Update(int playerid, [FromBody] JsonElement teamElement)
        {
            if (!teamElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE players SET ");
            var parameters = new Dictionary<string, object>
            {
                { "playerid", playerid } // 预设ID参数，用于WHERE子句
            };

            foreach (var property in teamElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        if (int.TryParse(property.Value.GetString(), out int teamId))
                        {
                            queryBuilder.Append("player_id = :player_id, ");
                            parameters.Add("player_id", teamId);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for player_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for player_id.");
                        }
                        break;
                    case "player_name":
                        queryBuilder.Append("player_name = :player_name, ");
                        parameters.Add("player_name", property.Value.GetString());
                        break;
                    case "birthday":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            queryBuilder.Append("birthday = :birthday, ");
                            parameters.Add("birthday", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for birthday: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for birthday.");
                        }
                        break;
                    case "team_id":
                        queryBuilder.Append("team_id = :team_id, ");
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "role":
                        queryBuilder.Append("role = :role, ");
                        parameters.Add("role", property.Value.GetString());
                        break;
                    case "used_foot":
                        queryBuilder.Append("used_foot = :used_foot, ");
                        parameters.Add("used_foot", property.Value.GetInt32());
                        break;
                    case "health_state":
                        queryBuilder.Append("health_state = :health_state, ");
                        parameters.Add("health_state", property.Value.GetInt32());
                        break;
                    case "rank":
                        queryBuilder.Append("rank = :rank, ");
                        parameters.Add("rank", property.Value.GetInt32());
                        break;
                    case "game_state":
                        queryBuilder.Append("game_state = :game_state, ");
                        parameters.Add("game_state", property.Value.GetInt32());
                        break;
                    case "trans_state":
                        queryBuilder.Append("trans_state = :trans_state, ");
                        parameters.Add("trans_state", property.Value.GetInt32());
                        break;
                    case "is_show":
                        queryBuilder.Append("is_show = :is_show, ");
                        parameters.Add("is_show", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            // 移除最后一个逗号和空格
            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE player_id = :playerid");
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
    }
}
