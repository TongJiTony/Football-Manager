using FootballManagerBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

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

        // GET v1/record/getbyRecord/{record_id}
        [HttpGet("getbyRecord/{record_id}")]
        public async Task<IActionResult> GetbyRecord(string record_id)
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
                r.record_id = :id";

            var parameters = new Dictionary<string, object> { { "id", record_id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }


        // POST v1/record/add
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

                    return CreatedAtAction(nameof(GetbyRecord), new { record_id = newRecord_id }, record);
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

        // PUT v1/record/update/{record_id}
        [HttpPut("update/{record_id}")]
        public async Task<IActionResult> UpdateRecord(int record_id, [FromBody] Record record)
        {
            try
            {
                string query = @"
                UPDATE records 
                SET 
                    team_id = :team_id,
                    transaction_date = :transaction_date,
                    amount = :amount,
                    description = :description
                WHERE 
                    record_id = :record_id";

                var parameters = new List<OracleParameter>
                {
                    new OracleParameter(":team_id", OracleDbType.Varchar2, record.team_id, ParameterDirection.Input),
                    new OracleParameter(":transaction_date", OracleDbType.Date, record.transaction_date, ParameterDirection.Input),
                    new OracleParameter(":amount", OracleDbType.Decimal, record.amount, ParameterDirection.Input),
                    new OracleParameter(":description", OracleDbType.Varchar2, record.description, ParameterDirection.Input),
                    new OracleParameter(":record_id", OracleDbType.Decimal, record_id, ParameterDirection.Input)
                };

                var result = await _context.ExecuteQueryWithParametersAsync(query, parameters);

                if (result.Count > 0)
                {
                    return Ok("记录更新成功。");
                }
                else
                {
                    return NotFound("未找到要更新的记录。");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"内部服务器错误: {ex.Message}");
            }
        }

        // DELETE v1/record/delete/{record_id}
        [HttpDelete("delete/{record_id}")]
        public async Task<IActionResult> DeleteRecord(int record_id)
        {
            try
            {
                string query = "DELETE FROM records WHERE record_id = :record_id";

                var parameters = new List<OracleParameter>
                {
                    new OracleParameter(":record_id", OracleDbType.Decimal, record_id, ParameterDirection.Input)
                };

                var result = await _context.ExecuteQueryWithParametersAsync(query, parameters);

                if (result.Count > 0)
                {
                    return Ok("记录删除成功。");
                }
                else
                {
                    return NotFound("未找到要删除的记录。");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"内部服务器错误: {ex.Message}");
            }
        }

        // 搜索函数，使用可变参数
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string team_id = null, [FromQuery] string transaction_date = null, [FromQuery] string min_amount = null, [FromQuery] string max_amount = null, [FromQuery] string description = null)
        {
            StringBuilder query = new StringBuilder(@"
            SELECT
                r.record_id,
                r.team_id,
                t.team_name,
                TO_CHAR(r.transaction_date, 'YYYY-MM') AS transaction_date,
                r.amount,
                r.description
            FROM
                records r
            JOIN
                teams t ON r.team_id = t.team_id
            WHERE 1 = 1");

            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(team_id))
            {
                query.Append(" AND r.team_id = :team_id");
                parameters.Add(new OracleParameter(":team_id", OracleDbType.Varchar2, team_id, ParameterDirection.Input));
            }

            if (!string.IsNullOrEmpty(transaction_date))
            {
                query.Append(" AND TO_CHAR(r.transaction_date, 'YYYY-MM-DD') = :transaction_date");
                parameters.Add(new OracleParameter(":transaction_date", OracleDbType.Varchar2, transaction_date, ParameterDirection.Input));
            }

            if (!string.IsNullOrEmpty(min_amount))
            {
                query.Append(" AND r.amount >= :min_amount");
                parameters.Add(new OracleParameter(":min_amount", OracleDbType.Decimal, min_amount, ParameterDirection.Input));
            }

            if (!string.IsNullOrEmpty(max_amount))
            {
                query.Append(" AND r.amount <= :max_amount");
                parameters.Add(new OracleParameter(":max_amount", OracleDbType.Decimal, max_amount, ParameterDirection.Input));
            }

            if (!string.IsNullOrEmpty(description))
            {
                query.Append(" AND r.description LIKE :description");
                parameters.Add(new OracleParameter(":description", OracleDbType.Varchar2, $"%{description}%", ParameterDirection.Input));
            }

            query.Append(" ORDER BY r.transaction_date");

            List<Dictionary<string, object>> result = await _context.ExecuteQueryWithParametersAsync(query.ToString(), parameters);
            return Ok(result);
        }
    }
}
