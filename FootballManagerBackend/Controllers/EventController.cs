using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text.Json;

namespace FootballManagerBackend.Controllers
{
    [Route("v1/event")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public EventController(OracleDbContext context)
        {
            _context = context;
        }

        // GET /v1/event/displayall
        [HttpGet("displayall")]
        public async Task<IActionResult> GetAll()
        {
            string query = @"
            SELECT 
                event_id, 
                match_id, 
                player_id, 
                player_name, 
                event_type, 
                TO_CHAR(event_time, 'YYYY-MM-DD HH24:MI:SS') AS event_time 
            FROM 
                events 
            NATURAL JOIN 
                players 
            ORDER BY 
                event_time";//发生时间以24小时制：年月日时分秒显示

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }

        // GET /v1/event/search
        [HttpGet("search")]
        public async Task<IActionResult> Search(
      [FromQuery] string? event_id = null,
      [FromQuery] string? match_id = null,
      [FromQuery] string? player_id = null,
      [FromQuery] string? event_type = null,
      [FromQuery] string? event_time = null)
        {
            var queryBuilder = new System.Text.StringBuilder(@"
    SELECT 
        event_id, 
        match_id, 
        player_id, 
        player_name,
        event_type, 
        TO_CHAR(event_time, 'YYYY-MM-DD HH24:MI:SS') AS event_time 
    FROM 
        events 
    NATURAL JOIN 
        players 
    WHERE 
        1=1");

            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(event_id))
            {
                queryBuilder.Append(" AND event_id = :event_id");
                parameters.Add("event_id", event_id);
            }

            if (!string.IsNullOrEmpty(match_id))
            {
                queryBuilder.Append(" AND match_id = :match_id");
                parameters.Add("match_id", match_id);
            }

            if (!string.IsNullOrEmpty(player_id))
            {
                queryBuilder.Append(" AND player_id = :player_id");
                parameters.Add("player_id", player_id);
            }

            if (!string.IsNullOrEmpty(event_type))
            {
                queryBuilder.Append(" AND event_type = :event_type");
                parameters.Add("event_type", event_type);
            }

            if (!string.IsNullOrEmpty(event_time))
            {
                queryBuilder.Append(" AND TO_CHAR(event_time, 'YYYY-MM-DD') = :event_time");
                parameters.Add("event_time", event_time);//搜索时，只需输入年月日即可显示当天的所有比赛事件
            }

            string query = queryBuilder.ToString();
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        // POST /v1/event/add
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] JsonElement eventElement)
        {
            string query = @"INSERT INTO events (event_id, match_id, player_id, event_type, event_time) VALUES (EVENT_SEQ.NEXTVAL, :match_id, :player_id, :event_type, :event_time) RETURNING event_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in eventElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "match_id":
                        parameters.Add("match_id", property.Value.GetInt32());
                        break;
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "event_type":
                        parameters.Add("event_type", property.Value.GetString());
                        break;
                    case "event_time":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("event_time", dateValue);//添加时，需要年月日时分秒，否则默认为00:00:00
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format for event_time: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for event_time.");
                        }
                        break;
                    default:
                        break;
                }
            }

            Console.WriteLine($"Generated Query: {query}");

            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newEventId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);
            return CreatedAtAction(nameof(GetAll), new { eventid = newEventId }, new { eventid = newEventId });
        }

        // DELETE /v1/event/delete?eventid=*
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string eventid)
        {
            string query = "DELETE FROM events WHERE event_id = :id";
            var parameters = new Dictionary<string, object> { { "id", eventid } };

            try
            {
                int result = await _context.ExecuteNonQueryAsync(query, parameters);
                if (result > 0)
                {
                    return Ok(new { message = "Event deleted successfully", eventid = eventid });
                }
                else
                {
                    return NotFound(new { message = "Event not found", eventid = eventid });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST /v1/event/update
        [HttpPost("update")]
        public async Task<IActionResult> Update(string eventid, [FromBody] JsonElement eventElement)
        {
            if (!eventElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE events SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in eventElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "match_id":
                        queryBuilder.Append("match_id = :match_id, ");
                        parameters.Add("match_id", property.Value.GetInt32());
                        break;
                    case "player_id":
                        queryBuilder.Append("player_id = :player_id, ");
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "event_type":
                        queryBuilder.Append("event_type = :event_type, ");
                        parameters.Add("event_type", property.Value.GetString());
                        break;
                    case "event_time":
                        queryBuilder.Append("event_time = :event_time, ");
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("event_time", dateValue);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid date format for event_time: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for event_time.");
                        }
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE event_id = :eventid");
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            parameters.Add("eventid", eventid);

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
