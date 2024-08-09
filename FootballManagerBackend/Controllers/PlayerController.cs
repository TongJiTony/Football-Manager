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
    [Route("v1/player")]
    public class PlayerController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public PlayerController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/player/displayall or GET /v1/player/displayall?teamid=* or GET /v1/player/displayall?lineupid=*
        public async Task<IActionResult> Get(int? teamid = null, int? lineupid = null)
        {
            string query = "SELECT player_id, player_name, TO_CHAR(birthday, 'YYYY-MM-DD') AS birthday, players.team_id AS team_id, team_name, role, used_foot, health_state, rank, game_state, trans_state, is_show, icon FROM players left outer join teams ON players.team_id = teams.team_id ORDER BY player_id";
            if (teamid != null)
            {
                query = "SELECT player_id, player_name, TO_CHAR(birthday, 'YYYY-MM-DD') AS birthday, players.team_id AS team_id, team_name, role, used_foot, health_state, rank, game_state, trans_state, is_show, icon FROM players left outer join teams ON players.team_id = teams.team_id WHERE players.team_id = :teamid ORDER BY player_id";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (lineupid != null)
            {
                query = @"SELECT player_id, player_name, TO_CHAR(birthday, 'YYYY-MM-DD') AS birthday, players.team_id AS team_id, team_name, role, used_foot, health_state, rank, game_state, trans_state, is_show, icon 
                FROM players left outer join teams ON players.team_id = teams.team_id, lineups 
                WHERE lineup_id = :lineupid AND 
                (player_id = player1_id OR player_id = player2_id OR player_id = player3_id OR 
                player_id = player4_id OR player_id = player5_id OR player_id = player6_id OR 
                player_id = player7_id OR player_id = player8_id OR player_id = player9_id OR
                player_id = player10_id OR player_id = player11_id) ORDER BY player_id";
                var parameters = new Dictionary<string, object> { { "lineupid", lineupid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("admin/displayall")] // GET /v1/player/displayall or GET /v1/player/displayall?page=*&limit=*&key=*
        public async Task<IActionResult> Get(int page = 1, int limit = 10, string key = "")
        {
            int startRow = (page - 1) * limit + 1;
            int endRow = page * limit;

            string query = @"
            SELECT * FROM (
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY player_id) AS rnum,
                    player_id, player_name, TO_CHAR(birthday, 'YYYY-MM-DD') AS birthday, 
                    players.team_id AS team_id, team_name, role, used_foot, health_state, 
                    rank, game_state, trans_state, is_show 
                FROM players 
                LEFT OUTER JOIN teams ON players.team_id = teams.team_id 
                WHERE players.player_name LIKE '%' || :key1 || '%' 
                   OR players.role LIKE '%' || :key2 || '%' 
                   OR teams.team_name LIKE '%' || :key3 || '%' 
            ) 
            WHERE rnum BETWEEN :startRow AND :endRow
            ORDER BY rnum";

            string countQuery = @"
            SELECT COUNT(*) AS total_count
            FROM players 
            LEFT OUTER JOIN teams ON players.team_id = teams.team_id 
            WHERE players.player_name LIKE '%' || :key1 || '%' 
               OR players.role LIKE '%' || :key2 || '%' 
               OR teams.team_name LIKE '%' || :key3 || '%'";

            var parameters = new Dictionary<string, object>
            {
                { "key1", key },
                { "key2", key },
                { "key3", key },
                { "startRow", startRow },
                { "endRow", endRow }
            };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            List<Dictionary<string, object>> countResult = await _context.ExecuteQueryAsync(countQuery, new Dictionary<string, object> { { "key1", key },{ "key2", key },{ "key3", key }});
            int totalCount = Convert.ToInt32(countResult[0]["TOTAL_COUNT"]);

            return Ok(new { data = result, total = totalCount });
        }


        [HttpGet("displayone")] // GET /v1/player/displayone?playerid=*
        public async Task<IActionResult> Get(int playerid)
        {
            string query = "SELECT player_id, player_name, TO_CHAR(birthday, 'YYYY-MM-DD') AS birthday, players.team_id AS team_id, team_name, role, used_foot, health_state, rank, game_state, trans_state, is_show, icon FROM players left outer join teams ON players.team_id = teams.team_id WHERE player_id = :Playerid";
            var parameters = new Dictionary<string, object> { { "Playerid", playerid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/player/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement playerElement)
        {
            string query = @"
            INSERT INTO players 
            (player_id, player_name, birthday, team_id, role, used_foot, health_state, rank, game_state, trans_state, is_show, icon) 
            VALUES 
            (PLAYER_SEQ.NEXTVAL, :player_name, :birthday, :team_id, :role, :used_foot, :health_state, :rank, :game_state, :trans_state, :is_show, :icon) 
            RETURNING player_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in playerElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
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
                            return BadRequest(new { message = $"Invalid date format for birthday: {property.Value.GetString()}" });
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
                    case "icon":
                        parameters.Add("icon", property.Value.GetString());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newPlayerId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(Get), new { PLAYER_ID = newPlayerId }, new { PLAYER_ID = newPlayerId });
        }

        [HttpPost("update")] // POST /v1/player/update?playerid=* + JSON
        public async Task<IActionResult> Update(int playerid, [FromBody] JsonElement playerElement)
        {
            if (!playerElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE players SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in playerElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        if (int.TryParse(property.Value.GetString(), out int player_id))
                        {
                            queryBuilder.Append("player_id = :player_id, ");
                            parameters.Add("player_id", player_id);
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
                    case "icon":
                        queryBuilder.Append("icon = :icon, ");
                        parameters.Add("icon", property.Value.GetString());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE player_id = :playerid");
            parameters.Add("player_id", playerid);
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

        [HttpDelete("delete")] // DELETE /v1/player/delete?playerid=*
        public async Task<IActionResult> Delete(int playerid)
        {
            string query = "DELETE FROM players WHERE player_id = :playerid";
            var parameters = new Dictionary<string, object> { { "playerid", playerid } };

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

        [HttpDelete("admin/delete")]
        public async Task<IActionResult> DeleteByIds([FromBody] int[] Playerids)
        {
            string query = "DELETE FROM players WHERE team_id = :id";
            try
            {
                int totalDeleted = 0;

                foreach (var Playerid in Playerids)
                {
                    var parameters = new Dictionary<string, object>
                    {
                        { "id", Playerid }
                    };

                    int result = await _context.ExecuteNonQueryAsync(query, parameters);

                    if (result > 0)
                    {
                        totalDeleted++;
                    }
                }

                if (totalDeleted > 0)
                {
                    return Ok(new { code=200,message = " Playerids deleted successfully", deletedCount = totalDeleted });
                }
                else
                {
                    return NotFound(new { message = "No  Playerids found for deletion" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
