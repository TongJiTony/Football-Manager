﻿using FootballManagerBackend.Models;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
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
        private readonly IConfiguration _configuration;

        public UserController(OracleDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        //GET v1/user/getInformation/{user_id}
        [HttpGet("getInformation/{user_id}")]
        public async Task<IActionResult> Get(string user_id)
        {
            string query = "SELECT * FROM users WHERE user_id = :id";
            var parameters = new Dictionary<string, object> { { "id", user_id } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
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
                string query = "SELECT user_password,user_right,user_name FROM users WHERE user_id = :id";
                var parameters = new Dictionary<string, object> { { "id", loginReq.user_id } };

                List<Dictionary<string, object>> rawResult = await _context.ExecuteQueryAsync(query, parameters);
                List<Dictionary<string, string>> result = rawResult.Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())).ToList();

                if (result != null && result.Count == 1)
                {
                    var userRecord = result[0];
                    if (userRecord.ContainsValue(loginReq.user_password))
                    {
                        string userId = loginReq.user_id.ToString();
                        // 用户验证成功，生成并返回令牌
                        string token = GenerateToken(userId, userRecord["USER_NAME"], userRecord["USER_RIGHT"]);
                        // 返回带有令牌的成功响应
                        var good_response = new
                        {
                            code = 200,
                            msg = "登录成功",
                            token = token
                        };
                        return Ok(good_response);
                    } 
                }
                var bad_response = new
                {
                    code = 500,
                    msg = "密码错误，登录失败",
                };
                return BadRequest(bad_response);
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

        // POST v1/user/changeAttributes
        [HttpPost("changeAttributes")]
        public async Task<IActionResult> PostChangeAttributes([FromBody] ChangeAttriRequest ChangeAttriReq)
        {
            try
            {
                // 构建更新 SQL 查询和参数
                string updateQuery = "UPDATE users SET user_name = :newName,  user_phone = :newPhone, icon = :newIcon WHERE user_id = :id";

                var updateParameters = new Dictionary<string, object>
                {
                    { "newName", ChangeAttriReq.new_name },
                    { "newPhone", ChangeAttriReq.new_phone },
                    { "newIcon", ChangeAttriReq.new_icon },
                    { "id", ChangeAttriReq.user_id }
                };

                // 执行更新操作
                int rowsUpdated = await _context.ExecuteNonQueryAsync(updateQuery, updateParameters);

                // 检查更新是否成功并返回相应结果
                if (rowsUpdated > 0)
                {
                    return Ok("User attributes updated successfully");
                }
                else
                {
                    return NotFound("User not found or no changes made.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE v1/user/delete
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteRequest DeleteReq)
        {
            try
            {
                // 查询用户的密码
                string query = "SELECT user_password FROM users WHERE user_id = :id";
                var parameters = new Dictionary<string, object> { { "id", DeleteReq.user_id } };

                List<Dictionary<string, object>> rawResult = await _context.ExecuteQueryAsync(query, parameters);
                List<Dictionary<string, string>> result = rawResult.Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())).ToList();

                if (result != null && result.Count == 1)
                {
                    if (result[0].ContainsValue(DeleteReq.user_password))
                    {
                        // 删除用户记录
                        string deleteQuery = "DELETE FROM users WHERE user_id = :id";
                        var deleteParameters = new Dictionary<string, object> { { "id", DeleteReq.user_id } };
                        int rowsDeleted = await _context.ExecuteNonQueryAsync(deleteQuery, deleteParameters);

                        if (rowsDeleted > 0)
                        {
                            return Ok("User deleted successfully."); // 204 No Content
                        }
                        else
                        {
                            return NotFound("User not found.");
                        }
                    }
                    return Unauthorized("Invalid password.");
                }
                return NotFound("User not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        private string GenerateToken(string userId, string userName, string userRight)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.Name, userName),
                new Claim(ClaimTypes.Role, userRight),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }

}

