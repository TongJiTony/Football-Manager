using Microsoft.AspNetCore.Mvc;
using FootballManagerBackend.Models;
using System.Numerics;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Text.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using Microsoft.AspNetCore.Http.Extensions;
using FootballManagerBackend.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace FootballManagerBackend.Controllers
{
    [Route("v1/agent")]
    [ApiController]
    public class AgentController : ControllerBase
    {
        private readonly OracleDbContext _context;
        private readonly Agent _agent;

        public AgentController(OracleDbContext context)
        {
            _agent = Agent.Instance;
            _context = context;
        }

        [HttpGet("status")]
        public IActionResult GetStatus(int? userid) // Get v1/agent/status?userid=*
        {
            //返回Agent状态，包括其连接状态、正在处理的用户
            Dictionary<string, object?> status = new Dictionary<string, object?>
            {
                { "connection_status", _agent.Connection_status },
                { "connection_user", _agent.Connection_user.ToString() },
            };
            //如果Agent正在处理本次查询用户的操作，可以返回用户的转会计划和合同计划，否则隐藏
            if (_agent.Connection_user == userid)
            {
                if (_agent.Trans_plan != null)
                {
                    foreach (var tuple in _agent.Trans_plan)
                    {
                        if (tuple.Key.ToLower() != "player_id" && tuple.Key.ToLower() != "contract_id") //去除重复属性
                        {
                            status.Add(tuple.Key.ToLower(), tuple.Value);
                        }
                    }
                }
                if (_agent.Cont_plan != null)
                {
                    foreach (var tuple in _agent.Cont_plan)
                    {
                        status.Add(tuple.Key.ToLower(), tuple.Value);
                    }
                }
            }
            return Ok(status);
        }

        [HttpOptions("connect")]
        public IActionResult Connect(int? userid) // Options v1/agent/connect?userid=*
        {
            //尝试连接Agent, 如果Agent被占用或用户ID未提供则返回400
            if (userid == null)
            {
                Response.StatusCode = 403;
                return Content("无法创建会话: 请提供您的用户名!");
            }
            if (_agent.Connection_status != "ready" && _agent.Connection_user != userid)
            {
                Response.StatusCode = 403;
                return Content("无法创建会话: 经纪人正在处理其他用户的转会申请!");
            }
            if (_agent.Connection_user == userid)
            {
                return Ok("您已在会话中了!");
            }

            _agent.UpdateConnectionStatus("connected");
            _agent.UpdateOperatingUser(userid);

            return Ok("已创建新会话，您可以提出转会申请了!");
        }

        [HttpOptions("disconnect")]
        public IActionResult Disconnect(int? userid) // Options v1/agent/disconnect?userid=*
        {
            //尝试断开Agent连接, 如果Agent被占用或用户ID未提供则返回400
            if (_agent.Connection_user == null)
            {
                return Ok("您已经结束对话了!");
            }
            if (userid == null)
            {
                Response.StatusCode = 403;
                return Content("无法结束会话: 请提供您的用户名!");
            }
            if (_agent.Connection_user != userid && _agent.Connection_status != "ready")
            {
                Response.StatusCode = 403;
                return Content("无法结束会话: 经纪人正在处理其他用户的转会申请!");
            }

            _agent.UpdateConnectionStatus("ready");
            _agent.UpdateOperatingUser(null);
            _agent.UpdateTransferPlan(null);
            _agent.UpdateContractPlan(null);

            return Ok("您已结束会话!");
        }

        [HttpOptions("newplan")]
        public async Task<IActionResult> Uploadnewplan(int? userid, [FromBody] JsonElement plan) // Options v1/agent/newplan?userid=*+JSON
        {
            if (_agent.Connection_status == "ready")
            {
                Response.StatusCode = 403;
                return Content("无法上传转会申请: 您还没有连接上您的转会经纪人!");
            }
            if (userid == null)
            {
                Response.StatusCode = 403;
                return Content("无法上传转会申请: 请提供您的用户名!");
            }
            if (_agent.Connection_user != userid)
            {
                Response.StatusCode = 403;
                return Content("无法上传转会申请: 经纪人正在处理其他用户的转会申请!");
            }

            _agent.UpdateContractPlan(null);
            _agent.Cont_plan = new Dictionary<string, object?>();
            foreach (var property in plan.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        _agent.Cont_plan.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id":
                        _agent.Cont_plan.Add("team_id", property.Value.GetInt32());
                        break;
                    case "start_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            _agent.Cont_plan.Add("start_date", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for start_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for start_date.");
                        }
                        break;
                    case "end_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue2))
                        {
                            _agent.Cont_plan.Add("end_date", dateValue2);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for end_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for end_date.");
                        }
                        break;
                    case "salary":
                        _agent.Cont_plan.Add("salary", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            _agent.UpdateTransferPlan(null);
            _agent.Trans_plan = new Dictionary<string, object?>();
            foreach (var property in plan.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        _agent.Trans_plan.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        _agent.Trans_plan.Add("team_id_from", property.Value.GetInt32());
                        break;
                    case "team_id_to":
                        _agent.Trans_plan.Add("team_id_to", property.Value.GetInt32());
                        break;
                    case "transfer_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            _agent.Trans_plan.Add("transfer_date", dateValue);
                        }
                        else
                        {
                            // 返回错误信息
                            Console.WriteLine($"Invalid date format for transfer_date: {property.Value.GetString()}");
                            return BadRequest("Invalid date format for transfer_date.");
                        }
                        break;
                    case "transfer_fees":
                        _agent.Trans_plan.Add("transfer_fees", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            //获取球员的评分、位置和年龄以便判断转会计划是否合理
            string query = @"SELECT rank, role, birthday FROM players WHERE player_id = :player_id";
            var parameters = new Dictionary<string, object>();
            foreach (var property in plan.EnumerateObject())
            {
                if (property.Name.ToLower() == "player_id")
                {
                    parameters.Add("player_id", property.Value.GetInt32());
                    break;
                }
            }
            int playerRank = 0;
            string? position = "";
            int age = 0;
            try
            {
                var result2 = await _context.ExecuteQueryAsync(query, parameters);
                playerRank = Convert.ToInt32(result2[0]["RANK"]);
                position = Convert.ToString(result2[0]["ROLE"]);
                age = DateTime.Now.Year - Convert.ToDateTime(result2[0]["BIRTHDAY"]).Year;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing OPTIONS request: {ex.Message}");
            }
            string[] result = _agent.JudgePlan(plan, playerRank, position, age);
            if (result[0] == "ok")
            {
                _agent.UpdateConnectionStatus("committed");
                return Ok("转会经纪人已同意，正在等待您的确认!");
            }
            else
            {
                _agent.UpdateTransferPlan(null);
                _agent.UpdateContractPlan(null);
                _agent.UpdateConnectionStatus("connected");
                Response.StatusCode = 400;
                return Content("转会经纪人已拒绝，原因是:" + result[1] + "。被拒绝的转会申请已经删除，您可以选择上传新的转会申请或结束会话!");
            }
        }

        [HttpOptions("confirm")]
        public async Task<IActionResult> Confirm(int? userid, int? confirm) // Options v1/agent/confirm?userid=*&confirm=*
        {
            //注意转会日期和合同开始日期是当天
            //尝试确认Agent的操作, 如果Agent未连接用户、用户ID未提供、Agent被占用或未提交计划则返回400
            if (_agent.Connection_user == null)
            {
                Response.StatusCode = 403;
                return Content("无法确认转会: 您还没有连接上您的转会经纪人!");
            }
            if (userid == null)
            {
                Response.StatusCode = 403;
                return Content("无法确认转会: 请提供您的用户名!");
            }
            if (_agent.Connection_user != userid)
            {
                Response.StatusCode = 403;
                return Content("无法确认转会: 经纪人正在处理其他用户的转会申请!");
            }
            if (_agent.Trans_plan == null || _agent.Cont_plan == null)
            {
                Response.StatusCode = 403;
                return Content("无法确认转会: 您还没有提交您的转会申请!");
            }

            if (confirm == 0)
            {
                //用户拒绝转会
                _agent.UpdateConnectionStatus("ready");
                _agent.UpdateOperatingUser(null);
                _agent.UpdateTransferPlan(null);
                _agent.UpdateContractPlan(null);
                return Ok("您已主动取消转会，本次转会申请已经删除，会话已自动结束!");
            }

            string jsonString = JsonSerializer.Serialize(_agent.Trans_plan);
            JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
            JsonElement jsonElement = jsonDocument.RootElement;

            //计算原先该球员在转出球队的薪水
            /*int oldSalary = 0;
            string query = @"SELECT salary FROM contracts WHERE player_id = :player_id AND team_id = :team_id";
            var parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            try
            {
                var result = await _context.ExecuteQueryAsync(query, parameters);
                oldSalary = Convert.ToInt32(result[0]["salary"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing OPTIONS request: {ex.Message}");
            }*/

            //删除原先该球员在转出球队的所有合同
            string query = @"DELETE FROM contracts WHERE player_id = :player_id AND team_id = :team_id";
            var parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    default:
                        break;
                }
            }

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }

            //原球队财务记录添加一条记录减少本月球员薪水支出
            /*query = @"INSERT INTO records (record_id, team_id, transaction_date, amount, description) VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)";
            parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "team_id_from")
                {
                    parameters.Add("team_id", property.Value.GetInt32());
                }
                if (property.Name.ToLower() == "start_date")
                {
                    if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                    {
                        parameters.Add("transaction_date", dateValue);
                    }
                    else
                    {
                        return BadRequest(new { message = $"Invalid date format for start_date: {property.Value.GetString()}" });
                    }
                    break;
                }
            }
            parameters.Add("amount", oldSalary);
            parameters.Add("description", "球员薪水");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }*/

            //添加一条原球队转会费收入记录
            query = @"INSERT INTO records (record_id, team_id, transaction_date, amount, description) VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)";
            parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "team_id_from")
                {
                    parameters.Add("team_id", property.Value.GetInt32());
                }
                if (property.Name.ToLower() == "transfer_date")
                {
                    if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                    {
                        parameters.Add("transaction_date", dateValue);
                    }
                    else
                    {
                        return BadRequest(new { message = $"Invalid date format for start_date: {property.Value.GetString()}" });
                    }
                }
                if (property.Name.ToLower() == "transfer_fees")
                {
                    parameters.Add("amount", property.Value.GetInt32());
                }
            }
            parameters.Add("description", "转会收入");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }
            
            //新球队财务记录添加一条记录增加本月球员薪水支出
            /*query = @"INSERT INTO records (record_id, team_id, transaction_date, amount, description) VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)";
            parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "team_id_to")
                {
                    parameters.Add("team_id", property.Value.GetInt32());
                }
                if (property.Name.ToLower() == "start_date")
                {
                    if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                    {
                        parameters.Add("transaction_date", dateValue);
                    }
                    else
                    {
                        return BadRequest(new { message = $"Invalid date format for start_date: {property.Value.GetString()}" });
                    }
                    break;
                }
                if (property.Name.ToLower() == "salary")
                {
                    parameters.Add("amount", -property.Value.GetInt32());
                }
            }
            parameters.Add("description", "球员薪水");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }*/

            //添加一条新球队转会费支出记录
            query = @"INSERT INTO records (record_id, team_id, transaction_date, amount, description) VALUES (RECORD_SEQ.NEXTVAL, :team_id, :transaction_date, :amount, :description)";
            parameters = new Dictionary<string, object>();
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "team_id_to")
                {
                    parameters.Add("team_id", property.Value.GetInt32());
                }
                if (property.Name.ToLower() == "transfer_date")
                {
                    if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                    {
                        parameters.Add("transaction_date", dateValue);
                    }
                    else
                    {
                        return BadRequest(new { message = $"Invalid date format for start_date: {property.Value.GetString()}" });
                    }
                }
                if (property.Name.ToLower() == "transfer_fees")
                {
                    parameters.Add("amount", -property.Value.GetInt32());
                }
            }
            parameters.Add("description", "转会支出");

            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }

            //添加新合同到数据库
            jsonString = JsonSerializer.Serialize(_agent.Cont_plan);
            jsonDocument = JsonDocument.Parse(jsonString);
            jsonElement = jsonDocument.RootElement;

            query = @"
            INSERT INTO contracts 
            (contract_id, player_id, team_id, start_date, end_date, salary) 
            VALUES 
            (CONTRACT_SEQ.NEXTVAL, :player_id, :team_id, :start_date, :end_date, :salary) 
            RETURNING contract_id INTO :new_id";

            parameters = new Dictionary<string, object>();
            var outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);

            foreach (var property in jsonElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id":
                        parameters.Add("team_id", property.Value.GetInt32());
                        break;
                    case "start_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue))
                        {
                            parameters.Add("start_date", dateValue);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for start_date: {property.Value.GetString()}" });
                        }
                        break;
                    case "end_date":
                        if (DateTime.TryParse(property.Value.GetString(), out DateTime dateValue2))
                        {
                            parameters.Add("end_date", dateValue2);
                        }
                        else
                        {
                            return BadRequest(new { message = $"Invalid date format for end_date: {property.Value.GetString()}" });
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
            
            jsonString = JsonSerializer.Serialize(_agent.Trans_plan);
            jsonDocument = JsonDocument.Parse(jsonString);
            jsonElement = jsonDocument.RootElement;

            //添加新转会记录到数据库
            query = @"
            INSERT INTO transfers 
            (transfer_id, contract_id, player_id, team_id_from, team_id_to, transfer_date, transfer_fees) 
            VALUES 
            (TRANSFER_SEQ.NEXTVAL, :contract_id, :player_id, :from, :to, :transfer_date, :transfer_fees) 
            RETURNING transfer_id INTO :new_id";

            parameters = new Dictionary<string, object>();
            outParameter = new OracleParameter("new_id", OracleDbType.Decimal, ParameterDirection.Output);
            parameters.Add("contract_id", newContractId);

            foreach (var property in jsonElement.EnumerateObject())
            {
                switch (property.Name.ToLower())
                {
                    case "player_id":
                        parameters.Add("player_id", property.Value.GetInt32());
                        break;
                    case "team_id_from":
                        parameters.Add("from", property.Value.GetInt32());
                        break;
                    case "team_id_to":
                        parameters.Add("to", property.Value.GetInt32());
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

            //更新球员的所属球队
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "team_id_to")
                {
                    var queryBuilder = new System.Text.StringBuilder("UPDATE players SET ");
                    parameters = new Dictionary<string, object>();
                    queryBuilder.Append("team_id = :team_id");
                    parameters.Add("team_id", property.Value.GetInt32());
                    queryBuilder.Append(" WHERE player_id = :playerid");
                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        if (prop.Name.ToLower() == "player_id")
                        {
                            parameters.Add("player_id", prop.Value.GetInt32());
                        }
                    }
                    query = queryBuilder.ToString();
                    Console.WriteLine($"Generated Query: {query}");

                    try 
                    {
                        await _context.ExecuteNonQueryAsync(query, parameters);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing OPTIONS request: {ex.Message}");
                    }
                    break;
                }
            }

            //删除原先包含转会球员的所有阵容
            int playerid = 0;
            foreach (var property in jsonElement.EnumerateObject())
            {
                if (property.Name.ToLower() == "player_id")
                {
                    playerid = property.Value.GetInt32();
                    break;
                }
            }
            query = @"DELETE FROM lineups WHERE player1_id = :playerid OR
                player2_id = :playerid OR player3_id = :playerid OR player4_id = :playerid OR
                player5_id = :playerid OR player6_id = :playerid OR player7_id = :playerid OR
                player8_id = :playerid OR player9_id = :playerid OR player10_id = :playerid OR
                player11_id = :playerid";
            parameters = new Dictionary<string, object> { { "playerid", playerid } };
            try
            {
                await _context.ExecuteNonQueryAsync(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing DELETE request: {ex.Message}");
            }

            _agent.UpdateConnectionStatus("ready");
            _agent.UpdateOperatingUser(null);
            _agent.UpdateTransferPlan(null);
            _agent.UpdateContractPlan(null);
            return Ok("您已确认转会，经纪人已经操作完成! 本次转会记录编号为" + newTransferId + "，合同编号为" + newContractId + "，祝贺您申请转会成功，会话已自动结束!");
        }
    }
}
