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

        //GET v1/record/displayone?record_id=*
        [HttpGet("displayone")]
        public async Task<IActionResult> Get(string record_id)
        {
            string query = "SELECT  FROM records WHERE record_id = :id";
            var parameters = new Dictionary<string, object> { { "id", record_id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        //GET v1/record/displayall
        [HttpGet("displayall")]
        public async Task<IActionResult> Get(int? team_id = null)
        {

            string query = @"
SELECT
    r.record_id,
    r.team_id,
    t.team_name,
    TO_CHAR(r.transaction_date, 'YYYY-MM-DD') AS transaction_date,
    r.amount,
    r.description
FROM
    records r
JOIN
    teams t ON r.team_id = t.team_id
WHERE
    r.team_id = :team_id
ORDER BY
    r.transaction_date";

            var parameters = new Dictionary<string, object> { { "id", team_id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);

        }

        // POST v1/record/add
        //ID从1000000000开始每次分配自动加一，其余信息由用户输入
        [HttpPost("add")]
        public async Task<IActionResult> PostRecordAdd([FromBody] Record record)
        {
            try
            {
                string query = @"
            INSERT INTO records (record_id, team_id, transaction_date, amount, description)
            VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)
            RETURNING record_id INTO :new_record_id";

                var parameters = new List<OracleParameter>
        {
            new OracleParameter(":team_id", OracleDbType.Varchar2, record.team_id, ParameterDirection.Input),
            new OracleParameter(":transaction_date", OracleDbType.Date, record.transaction_date, ParameterDirection.Input),
            new OracleParameter(":amount", OracleDbType.Decimal, record.amount, ParameterDirection.Input),
            new OracleParameter(":description", OracleDbType.Varchar2, record.description, ParameterDirection.Input),
            new OracleParameter(":new_record_id", OracleDbType.Decimal, ParameterDirection.Output)
        };

                var result = await _context.ExecuteQueryWithParametersAsync(query, parameters);

                if (parameters[4].Value != DBNull.Value)
                {
                    var oracleDecimal = (OracleDecimal)parameters[4].Value;
                    int newRecord_id = oracleDecimal.ToInt32();

                    record.record_id = newRecord_id;

                    return CreatedAtAction(nameof(PostRecordAdd), new { id = newRecord_id }, record);
                }
                else
                {
                    return BadRequest("插入记录失败。");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"内部服务器错误: {ex.Message}");
            }
        }

        // DELETE v1/record/delete?record_id=*
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteRecord(long record_id)
        {
            try
            {
                string query = "DELETE FROM records WHERE record_id = :record_id";
                var parameters = new Dictionary<string, object> { { "record_id", record_id } };
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
