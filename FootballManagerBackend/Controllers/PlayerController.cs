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
            string query = "SELECT * FROM players";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }

        [HttpGet("{Playerid}")]
        public async Task<IActionResult> Get(string Playerid)
        {
            string query = "SELECT * FROM players WHERE player_id = :Playerid";
            var parameters = new Dictionary<string, object> { { "Playerid", Playerid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] JsonElement playerElement)
        {
            string query = "INSERT INTO players (player_id, player_name, birthday, team_id, role, used_foot, health_state, rank, game_state, trans_state, is_show) VALUES (:id, :name, :checkdate, :team, :rolein, :foot, :health, :ranking, :game, :trans, :show)";

            var parameters = new Dictionary<string, object>();

            // 从 JsonElement 中获取值，并进行类型转换
            foreach (var property in playerElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("id", property.Value.GetInt32());
                        break;
                    case "player_name":
                        parameters.Add("name", property.Value.GetString());
                        break;
                    case "birthday":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("checkdate", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for birthday: {property.Value.GetString()}");

                        }
                        break;
                    case "team_id":
                        parameters.Add("team", property.Value.GetInt32());
                        break;
                    case "role":
                        parameters.Add("rolein", property.Value.GetString());
                        break;
                    case "used_foot":
                        parameters.Add("foot", property.Value.GetInt32());
                        break;
                    case "health_state":
                        parameters.Add("health", property.Value.GetInt32());
                        break;
                    case "rank":
                        parameters.Add("ranking", property.Value.GetInt32());
                        break;
                    case "game_state":
                        parameters.Add("game", property.Value.GetInt32());
                        break;
                    case "trans_state":
                        parameters.Add("trans", property.Value.GetInt32());
                        break;
                    case "is_show":
                        parameters.Add("show", property.Value.GetInt32());
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
