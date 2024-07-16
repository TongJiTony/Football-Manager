using FootballManagerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;


namespace FootballManagerBackend.Controllers
{
    [Route("v1/record")]
    [ApiController]
    public class RecordController : ControllerBase
    {
        private readonly OracleDbContext _context;
        private readonly IConfiguration _configuration;

        public RecordController(OracleDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //GET v1/user/getInformation/{record_id}
        [HttpGet("getInformation/{record_id}")]
        public async Task<IActionResult> Get(string record_id)
        {
            string query = "SELECT * FROM records WHERE record_id = :id";
            var parameters = new Dictionary<string, object> { { "id", record_id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        // POST v1/record/add
        //ID从1000000000开始每次分配自动加一，其余信息由用户输入
        [HttpPost("add")]
        public async Task<IActionResult> PostRecordAdd(Record record)
        {
            try
            {
                string query = @"
                INSERT INTO records (record_id, team_id, transaction_date, amount, description)
                VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)
                RETURNING record_id INTO :new_record_id";

                var parameters = new List<OracleParameter>
                {
                    new OracleParameter(":team_id", record.team_id),
                    new OracleParameter(":transaction_date", record.transaction_date),
                    new OracleParameter(":amount", record.amount),
                    new OracleParameter(":description", record.description),
                    new OracleParameter(":new_record_id", OracleDbType.Decimal, ParameterDirection.Output)
                };

                var result = await _context.ExecuteQueryWithParametersAsync(query, parameters);

                if (parameters[5].Value != DBNull.Value)
                {
                    var oracleDecimal = (OracleDecimal)parameters[5].Value;
                    int newRecord_id = oracleDecimal.ToInt32();

                    record.record_id = newRecord_id;

                    return CreatedAtAction(nameof(PostRecordAdd), new { id = newRecord_id }, record);
                }
                else
                {
                    return BadRequest("Failed to insert record.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/<RecordController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<RecordController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<RecordController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
