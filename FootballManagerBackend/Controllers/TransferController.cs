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
    [Route("v1/transfer")]
    public class TransferController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public TransferController(OracleDbContext context)
        {
            _context = context;
        }

        [HttpGet("displayall")] // GET /v1/transfer/displayall or GET /v1/transfer/displayall?teamid=* or GET /v1/transfer/displayall?fromteamid=* or GET /v1/transfer/displayall?toteamid=* or GET /v1/transfer/displayall?playerid=*
        public async Task<IActionResult> GetAll(int? teamid = null, int? fromteamid = null, int? toteamid = null, int? playerid = null)
        {
            string query;
            if (playerid != null)
            {
                query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                WHERE transfers.player_id = :playerid
                ORDER BY transfer_id";
                var parameters = new Dictionary<string, object> { { "playerid", playerid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (teamid != null)
            {
                query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                WHERE transfers.team_id_from = :teamid OR transfers.team_id_to = :teamid 
                ORDER BY transfer_id";
                var parameters = new Dictionary<string, object> { { "teamid", teamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (fromteamid != null)
            {
                query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                WHERE transfers.team_id_from = :fromteamid 
                ORDER BY transfer_id";
                var parameters = new Dictionary<string, object> { { "fromteamid", fromteamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else if (toteamid != null)
            {
                query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                WHERE transfers.team_id_to = :toteamid 
                ORDER BY transfer_id";
                var parameters = new Dictionary<string, object> { { "toteamid", toteamid } };
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
                return Ok(result);
            }
            else
            {
                query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                ORDER BY transfer_id";
                List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
                return Ok(result);
            }
        }

        [HttpGet("displayone")] // GET /v1/transfer/displayone?transferid=*
        public async Task<IActionResult> GetOne(int transferid)
        {
            string query = @"SELECT transfer_id, contract_id, transfers.player_id AS player_id, 
                players.player_name AS player_name, transfers.team_id_from AS team_id_from, 
                fromteam.team_name AS team_name_from, transfers.team_id_to AS team_id_to, 
                toteam.team_name AS team_name_to, transfer_date, transfer_fees 
                FROM transfers JOIN players ON transfers.player_id = players.player_id 
                JOIN teams fromteam ON fromteam.team_id = transfers.team_id_from 
                JOIN teams toteam ON toteam.team_id = transfers.team_id_to 
                WHERE transfer_id = :transferid";
            var parameters = new Dictionary<string, object> { { "transferid", transferid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        [HttpPost("add")] // POST /v1/transfer/add + JSON
        public async Task<IActionResult> Add([FromBody] JsonElement transferElement)
        {
            string query = @"
            INSERT INTO transfers 
            (transfer_id, contract_id, player_id, team_id_from, team_id_to, transfer_date, transfer_fees) 
            VALUES 
            (TRANSFER_SEQ.NEXTVAL, :contract_id, :player_id, :team_id_from, :team_id_to, :transfer_date, :transfer_fees) 
            RETURNING transfer_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in transferElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "contract_id":
                        parameters.Add("contract_id", property.Value.GetInt32());
                        break;
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        parameters.Add("team_id_from", property.Value.GetInt32());
                        break;
                    case "team_id_to":
                        parameters.Add("team_id_to", property.Value.GetInt32());
                        break;
                    case "transfer_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("transfer_date", dateValue);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for transfer_date: {property.Value.GetString()}" });
                        }
                        break;
                    case "transfer_fees":
                        parameters.Add("transfer_fees", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newTransferId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);

            return CreatedAtAction(nameof(GetOne), new { TRANSFER_ID = newTransferId }, new { TRANSFER_ID = newTransferId });
        }

        [HttpPost("update")] // POST /v1/transfer/update?transferid=* + JSON
        public async Task<IActionResult> Update(int transferid, [FromBody] JsonElement transferElement)
        {
            if (!transferElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE transfers SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in transferElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "transfer_id":
                        if (int.TryParse(property.Value.GetString(), out int contract_id))
                        {
                            queryBuilder.Append("transfer_id = :transfer_id, ");
                            parameters.Add("transfer_id", contract_id);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for transfer_id: {property.Value.GetString()}");
                            return BadRequest("Invalid format for transfer_id.");
                        }
                        break;
                    case "contract_id":
                        queryBuilder.Append("contract_id = :contract_id, ");
                        parameters.Add("contract_id", property.Value.GetInt32());
                        break;
                    case "player_id":
                        queryBuilder.Append("player_id = :player_id, ");
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        queryBuilder.Append("team_id_from = :team_id_from, ");
                        parameters.Add("team_id_from", property.Value.GetInt32());
                        break;
                    case "team_id_to":
                        queryBuilder.Append("team_id_to = :team_id_to, ");
                        parameters.Add("team_id_to", property.Value.GetInt32());
                        break;
                    case "transfer_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            queryBuilder.Append("transfer_date = :transfer_date, ");
                            parameters.Add("transfer_date", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for transfer_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for transfer_date.");
                        }
                        break;
                    case "transfer_fees":
                        queryBuilder.Append("transfer_fees = :transfer_fees, ");
                        parameters.Add("transfer_fees", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE transfer_id = :transferid");
            parameters.Add("transferid", transferid);
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

        [HttpDelete("delete")] // DELETE /v1/transfer/delete?transferid=*
        public async Task<IActionResult> Delete(int transferid)
        {
            string query = "DELETE FROM transfers WHERE transfer_id = :transferid";
            var parameters = new Dictionary<string, object> { { "transferid", transferid } };

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
