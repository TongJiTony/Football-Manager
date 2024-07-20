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
    [Route("v1/contract")]
    public class ContractController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public ContractController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/contract/displayall or GET /v1/contract/displayall?teamid=* or GET /v1/contract/displayall?playerid=*
        public async Task<IActionResult> GetAll(int? teamid = null, int? playerid = null)
        {
            string query;
            if (playerid != null)
            {
                query = @"SELECT contract_id, contracts.player_id AS player_id, players.player_name AS player_name, contracts.team_id AS team_id, teams.team_name AS team_name, start_date, end_date, salary 
                FROM contracts join players on contracts.player_id = players.player_id join teams on contracts.team_id = teams.team_id WHERE contracts.player_id = :playerid ORDER BY contract_id";
                var parameters = new Dictionary<string, object> { { "playerid", playerid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (teamid != null)
            {
                query = @"SELECT contract_id, contracts.player_id AS player_id, players.player_name AS player_name, contracts.team_id AS team_id, teams.team_name AS team_name, start_date, end_date, salary 
                FROM contracts join players on contracts.player_id = players.player_id join teams on contracts.team_id = teams.team_id WHERE contracts.team_id = :teamid ORDER BY contract_id";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                query = @"SELECT contract_id, contracts.player_id AS player_id, players.player_name AS player_name, contracts.team_id AS team_id, teams.team_name AS team_name, start_date, end_date, salary 
                FROM contracts join players on contracts.player_id = players.player_id join teams on contracts.team_id = teams.team_id ORDER BY contract_id";
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("displayone")] // GET /v1/contract/displayone?contractid=*
        public async Task<IActionResult> GetOne(int contractid)
        {
            string query = @"SELECT contract_id, contracts.player_id AS player_id, players.player_name AS player_name, contracts.team_id AS team_id, teams.team_name AS team_name, start_date, end_date, salary 
                FROM contracts join players on contracts.player_id = players.player_id join teams on contracts.team_id = teams.team_id WHERE contract_id = :contractid";
            var parameters = new Dictionary<string, object> { { "contractid", contractid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/contract/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement contractElement)
        {
            string query = @"
            INSERT INTO contracts 
            (contract_id, player_id, team_id, start_time, end_time, salary) 
            VALUES 
            (CONTRACT_SEQ.NEXTVAL, :player_id, :team_id, :start_time, :end_time, :salary) 
            RETURNING contract_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in contractElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "start_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("start_time", dateValue);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for start_time: {property.Value.GetString()}" });
                        }
                        break;
                    case "end_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue2))
                        {
                            parameters.Add("end_time", dateValue2);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for end_time: {property.Value.GetString()}" });
                        }
                        break;
                    case "salary":
                        parameters.Add("salary", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newContractId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(GetOne), new { CONTRACT_ID = newContractId }, new { CONTRACT_ID = newContractId });
        }

        [HttpPost("update")] // POST /v1/contract/update?contractid=* + JSON
        public async Task<IActionResult> Update(int contractid, [FromBody] JsonElement contractElement)
        {
            if (!contractElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE contracts SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in contractElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "contract_id":
                        if (int.TryParse(property.Value.GetString(), out int contract_id))
                        {
                            queryBuilder.Append("contract_id = :contract_id, ");
                            parameters.Add("contract_id", contract_id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for contract_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for contract_id.");
                        }
                        break;
                    case "player_id":
                        queryBuilder.Append("player_id = :player_id, ");
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id":
                        queryBuilder.Append("team_id = :team_id, ");
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "start_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            queryBuilder.Append("start_time = :start_time, ");
                            parameters.Add("start_time", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for start_time: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for start_time.");
                        }
                        break;
                    case "end_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue2))
                        {
                            queryBuilder.Append("end_time = :end_time, ");
                            parameters.Add("end_time", dateValue2);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for end_time: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for end_time.");
                        }
                        break;
                    case "salary":
                        queryBuilder.Append("salary = :salary, ");
                        parameters.Add("salary", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE contract_id = :contractid");
            parameters.Add("contractid", contractid);
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

        [HttpDelete("delete")] // DELETE /v1/contract/delete?contractid=*
        public async Task<IActionResult> Delete(int contractid)
        {
            string query = "DELETE FROM contracts WHERE contract_id = :contractid";
            var parameters = new Dictionary<string, object> { { "contractid", contractid } };

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
