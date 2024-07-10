﻿using FootballManagerBackend.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerBackend.Controllers
{
    [Route("v1/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly OracleDbContext _context;


        public UserController(OracleDbContext context)
        {
            _context = context;
        }

        // POST v1/user/add
        //添加新用户，用户ID从1000000000开始每次分配自动加一，其余信息由用户输入
        //其余信息可能需要合法性检验
        [HttpPost("add")]
        public async Task<IActionResult> PostUserAdd(User user)
        {
            try
            {
                string query = @"
INSERT INTO users (user_id, user_name, user_right, user_password, user_phone, icon)
VALUES (USER_SEQ.NEXTVAL, :userName, :userRight, :userPassword, :userPhone, :icon)
RETURNING user_id INTO :new_user_id";

                var parameters = new List<OracleParameter>
{
new OracleParameter(":userName", user.UserName),
new OracleParameter(":userRight", user.UserRight),
new OracleParameter(":userPassword", user.UserPassword),
new OracleParameter(":userPhone", user.UserPhone),
new OracleParameter(":icon", user.Icon),
new OracleParameter(":new_user_id", OracleDbType.Decimal, ParameterDirection.Output)
};

                var result = await _context.ExecuteQueryWithParametersAsync(query, parameters);

                if (parameters[5].Value != DBNull.Value)
                {
                    var oracleDecimal = (OracleDecimal)parameters[5].Value;
                    int newUserId = oracleDecimal.ToInt32();

                    user.UserId = newUserId;

                    return CreatedAtAction(nameof(PostUserAdd), new { id = newUserId }, user);
                }
                else
                {
                    return BadRequest("Failed to insert user.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        //POST v1/user/login
        [HttpPost("login")]
        public async Task<IActionResult> PostUserLogin([FromBody] FootballManagerBackend.Models.LoginRequest loginReq)
        {
            try
            {
                string query = "SELECT user_password FROM users WHERE user_id = :id";
                var parameters = new Dictionary<string, object> { { "id", loginReq.user_id } };

                List<Dictionary<string, object>> rawResult = await _context.ExecuteQueryAsync(query, parameters);
                List<Dictionary<string, string>> result = rawResult.Select(d =>    d.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())).ToList();

                if (result != null && result.Count == 1)
                {
                    if (result[0].ContainsValue(loginReq.user_password))
                    {
                        return Ok("Authentication successful");
                    }
                }
                return BadRequest("Authentication failed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //POST v1/user/changePassword
        [HttpPost("changePassword")]
        public async Task<IActionResult> PostUserChangePassword([FromBody] FootballManagerBackend.Models.ChangePasswordRequest ChangePasswordReq)
        {
            try
            {
                string query = "SELECT user_password FROM users WHERE user_id = :id";
                var parameters = new Dictionary<string, object> { { "id", ChangePasswordReq.user_id } };

                List<Dictionary<string, object>> rawResult = await _context.ExecuteQueryAsync(query, parameters);
                List<Dictionary<string, string>> result = rawResult.Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())).ToList();

                if (result != null && result.Count == 1)
                {
                    if (result[0].ContainsValue(ChangePasswordReq.user_password))
                    {
                        //根据user_id在users表中寻找对应记录，修改其中的user_password，改为new_password
                        string updateQuery = "UPDATE users SET user_password = :newPassword WHERE user_id = :id";
                        var updateParameters = new Dictionary<string, object>
                {
                    { "newPassword", ChangePasswordReq.new_password }, // 使用 ChangePasswordReq 中的 new_password 属性来存放新密码
                    { "id", ChangePasswordReq.user_id }
                };

                        // 执行异步命令，更新数据库中的用户密码
                        int rowsUpdated = await _context.ExecuteNonQueryAsync(updateQuery, updateParameters);

                        // 检查是否成功更新密码并返回相应的结果
                        if (rowsUpdated > 0)
                        {
                            return Ok("Password changed successfully");
                        }
                        else
                        {
                            return StatusCode(500, "Failed to update password");
                        }
                    }
                }
                return BadRequest("Authentication failed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }













    }

}

