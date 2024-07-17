using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Text.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FootballManagerBackend.Controllers
{
    [Route("v1/stadium")]
    [ApiController]
    public class StadiumController : ControllerBase
    {
        private readonly OracleDbContext _context;

        public StadiumController(OracleDbContext context)
        {
            _context = context;
        }
        // GET /v1/stadium/displayall or GET /v1/stadium/displayall?stadiumid=*
        [HttpGet("displayall")]
        public async Task<IActionResult> Get()
        {
            string query = "SELECT * FROM stadiums ORDER BY stadium_name";
            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query);
            return Ok(result);
        }
        [HttpGet("displayone")]
        public async Task<IActionResult> Get(string Stadiumid)
        {
            string query = "SELECT * FROM stadiums WHERE stadium_id = :id";
            var parameters = new Dictionary<string, object> { { "id", Stadiumid } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }
        // POST /v1/stadium/add
        [HttpPost("add")]
        public async Task<IActionResult> Post([FromBody] JsonElement stadiumElement)
        {
            string query = @"INSERT INTO stadiums (stadium_name, stadium_capacity, stadium_location, stadium_id) 
                             VALUES (:name, :capacity, :location, STADIUM_SEQ.NEXTVAL) RETURNING stadium_id INTO :new_id";

            var parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in stadiumElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "stadium_name":
                        parameters.Add("name", property.Value.GetString());
                        break;
                    case "stadium_capacity":
                        parameters.Add("capacity", property.Value.GetInt32());
                        break;
                    case "stadium_location":
                        parameters.Add("location", property.Value.GetString());
                        break;
                    default:
                        break;
                }
            }

            Console.WriteLine($"Generated Query: {query}");
            await _context.ExecuteNonQueryAsyncForAdd(query, parameters, outParameter);
            int newStadiumId = Convert.ToInt32(((OracleDecimal)outParameter.Value).Value);
            return CreatedAtAction(nameof(Get), new { stadiumid = newStadiumId }, new { stadiumid = newStadiumId });
        }

        // DELETE /v1/stadium/delete
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(string stadiumid)
        {
            string query = "DELETE FROM stadiums WHERE stadium_id = :id";
            var parameters = new Dictionary<string, object> { { "id", stadiumid } };

            try
            {
                int result = await _context.ExecuteNonQueryAsync(query, parameters);
                if (result > 0)
                {
                    return Ok(new { message = "Stadium deleted successfully", stadiumid = stadiumid });
                }
                else
                {
                    return NotFound(new { message = "Stadium not found", stadiumid = stadiumid });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
        // POST /v1/stadium/update
        [HttpPost("update")]
        public async Task<IActionResult> Update(string updateStadiumid, [FromBody] JsonElement stadiumElement)
        {
            if (!stadiumElement.EnumerateObject().Any())
            {
                return BadRequest("No fields provided for update.");
            }

            var queryBuilder = new System.Text.StringBuilder("UPDATE stadiums SET ");
            var parameters = new Dictionary<string, object>();

            foreach (var property in stadiumElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "stadium_name":
                        queryBuilder.Append("stadium_name = :stadium_name, ");
                        parameters.Add("stadium_name", property.Value.GetString());
                        break;
                    case "stadium_capacity":
                        if (int.TryParse(property.Value.GetString(), out int capacityValue))
                        {
                            queryBuilder.Append("stadium_capacity = :stadium_capacity, ");
                            parameters.Add("stadium_capacity", capacityValue);
                        }
                        else
                        {
                            Console.WriteLine($"Invalid format for stadium_capacity: {property.Value.GetString()}");
                            return BadRequest("Invalid format for stadium_capacity.");
                        }
                        break;
                    case "stadium_location":
                        queryBuilder.Append("stadium_location = :stadium_location, ");
                        parameters.Add("stadium_location", property.Value.GetString());
                        break;
                    default:
                        break;
                }
            }

            queryBuilder.Length -= 2;
            queryBuilder.Append(" WHERE stadium_id = :updateStadiumid");
            string query = queryBuilder.ToString();
            Console.WriteLine($"Generated Query: {query}");

            parameters.Add("updateStadiumid", updateStadiumid);

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
