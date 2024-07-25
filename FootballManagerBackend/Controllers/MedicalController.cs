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
    [Route("v1/medical")]
    public class MedicalController : ControllerBase
    {
        //类似playerlist开发模式，做成列表形式，具有返回上级功能，添加功能，删除功能，修改功能，
        //查询功能，增改删采用弹窗的方式完成
        private readonly OracleDbContext _context;

        public MedicalController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/medical/displayall or GET /v1/medical/displayall?teamid=* or GET /v1/medical/displayall?playerid=* or GET /v1/medical/displayall?lineupid=*
        public async Task<IActionResult> GetAll(int? teamid = null, int? playerid = null, int? lineupid = null)
        {
            string query;
            if (playerid != null)
            {
                query = @"SELECT medical_id, player_id, player_name, team_id, team_name, hurt_part, hurt_time, medical_care, state 
                FROM medicals natural join players natural join teams WHERE player_id = :playerid";
                var parameters = new Dictionary<string, object> { { "playerid", playerid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (lineupid != null)
            {
                query = @"SELECT medical_id, player_id, player_name, lineups.team_id AS team_id, team_name, hurt_part, hurt_time, medical_care, state 
                FROM medicals natural join players natural join teams, lineups 
                WHERE lineup_id = :lineupid AND 
                (player_id = player1_id OR player_id = player2_id OR player_id = player3_id OR 
                player_id = player4_id OR player_id = player5_id OR player_id = player6_id OR 
                player_id = player7_id OR player_id = player8_id OR player_id = player9_id OR
                player_id = player10_id OR player_id = player11_id)";
                var parameters = new Dictionary<string, object> { { "lineupid", lineupid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (teamid != null)
            {
                query = @"SELECT medical_id, player_id, player_name, team_id, team_name, hurt_part, hurt_time, medical_care, state 
                FROM medicals natural join players natural join teams WHERE team_id = :teamid";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                query = @"SELECT medical_id, player_id, player_name, team_id, team_name, hurt_part, hurt_time, medical_care, state 
                FROM medicals natural join players natural join teams";
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("displayone")] // GET /v1/medical/displayone?medicalid=*
        public async Task<IActionResult> GetOne(int medicalid)
        {
            string query = @"SELECT medical_id, player_id, player_name, team_id, team_name, hurt_part, hurt_time, medical_care, state 
                FROM medicals natural join players natural join teams WHERE medical_id = :medicalid";
            var parameters = new Dictionary<string, object> { { "medicalid", medicalid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/medical/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement medicalElement)
        {
            string query = @"
            INSERT INTO medicals 
            (medical_id, player_id, hurt_part, hurt_time, medical_care, state) 
            VALUES 
            (LINEUP_SEQ.NEXTVAL, :player_id, :hurt_part, :hurt_time, :medical_care, :state) 
            RETURNING medical_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in medicalElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "hurt_part":
                        parameters.Add("hurt_part", property.Value.GetString());
                        break;
                    case "hurt_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("hurt_time", dateValue);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for hurt_time: {property.Value.GetString()}" });
                        }
                        break;
                    case "medical_care":
                        parameters.Add("medical_care", property.Value.GetString());
                        break;
                    case "state":
                        parameters.Add("state", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newMedicalId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(GetOne), new { MEDICAL_ID = newMedicalId }, new { MEDICAL_ID = newMedicalId });
        }

        [HttpPost("update")] // POST /v1/medical/update?medicalid=* + JSON
        public async Task<IActionResult> Update(int medicalid, [FromBody] JsonElement medicalElement)
        {
            if (!medicalElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE medicals SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in medicalElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "medical_id":
                        if (int.TryParse(property.Value.GetString(), out int medical_id))
                        {
                            queryBuilder.Append("medical_id = :medical_id, ");
                            parameters.Add("medical_id", medical_id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for medical_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for medical_id.");
                        }
                        break;
                    case "player_id":
                        queryBuilder.Append("player_id = :player_id, ");
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "hurt_part":
                        queryBuilder.Append("hurt_part = :hurt_part, ");
                        parameters.Add("hurt_part", property.Value.GetString());
                        break;
                    case "hurt_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            queryBuilder.Append("hurt_time = :hurt_time, ");
                            parameters.Add("hurt_time", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for hurt_time: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for hurt_time.");
                        }
                        break;
                    case "medical_care":
                        queryBuilder.Append("medical_care = :medical_care, ");
                        parameters.Add("medical_care", property.Value.GetString());
                        break;
                    case "state":
                        queryBuilder.Append("state = :state, ");
                        parameters.Add("state", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE medical_id = :medicalid");
            parameters.Add("medicalid", medicalid);
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

        [HttpDelete("delete")] // DELETE /v1/medical/delete?medicalid=*
        public async Task<IActionResult> Delete(int medicalid)
        {
            string query = "DELETE FROM medicals WHERE medicalid = :medicalid";
            var parameters = new Dictionary<string, object> { { "medicalid", medicalid } };

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
