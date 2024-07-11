using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("add")] // POST /v1/player/add
        public async Task<IActionResult> Post([FromBody] JsonElement playerElement)
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
    }
}
