using FootballManagerBackend.Models;
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
using System.Security.Principal;
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

        // GET /v1/user/displayall
        [HttpGet("admin/displayall")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string key = "")
        {
            int startRow = (page - 1) * limit + 1;
            int endRow = page * limit;

            string query = @"
            SELECT * FROM (
                SELECT 
                    u.user_id, 
                    u.user_name, 
                    u.user_right, 
                    u.user_password, 
                    u.user_phone, 
                    u.icon,
                    ROW_NUMBER() OVER (ORDER BY u.user_id) AS rnum
                FROM 
                    users u
                WHERE 
                    u.user_name LIKE '%' || :key || '%' 
                    OR u.user_phone LIKE '%' || :key2 || '%'
            ) 
            WHERE rnum BETWEEN :startRow AND :endRow";

            string countQuery = @"
    SELECT COUNT(*) AS total_count
    FROM users u
    WHERE  u.user_name LIKE '%' || :key || '%' 
                    OR u.user_phone LIKE '%' || :key2 || '%'";

            var parameters = new Dictionary<string, object>
            {
                { "key", key },
                { "key2", key },
                { "startRow", startRow },
                { "endRow", endRow }
            };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            List<Dictionary<string, object>> countResult = await _context.ExecuteQueryAsync(countQuery, new Dictionary<string, object> { { "key", key }, { "key2", key }});
            int totalCount = Convert.ToInt32(countResult[0]["TOTAL_COUNT"]);

            return Ok(new { data = result, total = totalCount });
        }

        // GET /v1/user/displayone?userId=*
        [HttpGet("displayone")]
        public async Task<IActionResult> Get(int userId)
        {
            string query = "SELECT * FROM users WHERE user_id = :id";
            var parameters = new Dictionary<string, object> { { "id", userId } };

            List<Dictionary<string, object>> result = await _context.ExecuteQueryAsync(query, parameters);
            return Ok(result);
        }

        // GET /v1/user/getDeleteImage?userId=*
        [HttpGet("getDeleteImage")]
        public async Task<IActionResult> GetDeleteImage(int userId)
        {
            string query = "SELECT delete_icon FROM users WHERE user_id = :id";
            var parameters = new Dictionary<string, object> { { "id", userId } };

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

                    // 创建响应对象
                    var response = new
                    {
                        code = 200,
                        msg = "注册成功",
                        user_id = newUserId,
                        user_name = user.UserName,
                        user_right = user.UserRight,
                        user_password = user.UserPassword,
                        user_phone = user.UserPhone,
                        icon = user.Icon
                    };

                    return CreatedAtAction(nameof(PostUserAdd), new { id = newUserId }, response);
                }
                else
                {
                    var bad_response = new
                    {
                        code = 500,
                        msg = "注册失败",
                    };
                    return BadRequest(bad_response);
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
                    var bad_response2 = new
                    {
                        code = 400,
                        msg = "密码错误，请重新输入",
                    };
                    return BadRequest(bad_response2);
                }
                var bad_response1 = new
                {
                    code = 401,
                    msg = "该用户不存在",
                };
                return BadRequest(bad_response1);
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
                            var good_response = new
                            {
                                code = 200,
                                msg = "密码修改成功",
                            };
                            return Ok(good_response);
                        }
                        else
                        {
                            var good_response = new
                            {
                                code = 300,
                                msg = "密码修改失败，请重试",
                            };
                            return Ok(good_response);
                        }
                    }
                }
                var bad_response = new
                {
                    code = 500,
                    msg = "密码错误，修改失败",
                };
                return BadRequest(bad_response);
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
                    var good_response = new
                    {
                        code = 200,
                        msg = "修改成功",
                    };
                    return Ok(good_response);
                }
                else
                {
                    var good_response = new
                    {
                        code = 300,
                        msg = "用户不存在或无修改发生",
                    };
                    return NotFound(good_response);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST v1/user/admin/changeAttributes
        [HttpPost("admin/changeAttributes")]
        public async Task<IActionResult> PostChangeAll([FromBody] User user)
        {
            try
            {
                // 构建更新 SQL 查询和参数
                string updateQuery = "UPDATE users SET user_name = :newName,  user_phone = :newPhone, icon = :newIcon , user_right = :newRight , user_password = :newPwd WHERE user_id = :id";

                var updateParameters = new Dictionary<string, object>
                {
                    { "newName", user.UserName },
                    { "newPhone", user.UserPhone },
                    { "newIcon", user.Icon },
                    { "newRight", user.UserRight},
                    {"newPwd", user.UserPassword },
                    { "id", user.UserId }
                };

                // 执行更新操作
                int rowsUpdated = await _context.ExecuteNonQueryAsync(updateQuery, updateParameters);

                // 检查更新是否成功并返回相应结果
                if (rowsUpdated > 0)
                {
                    var good_response = new
                    {
                        code = 200,
                        msg = "修改成功",
                    };
                    return Ok(good_response);
                }
                else
                {
                    var good_response = new
                    {
                        code = 300,
                        msg = "用户不存在或无修改发生",
                    };
                    return NotFound(good_response);
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

        // DELETE v1/user/admin/delete
        [HttpDelete("admin/delete")]
        public async Task<IActionResult> DeleteUsers([FromBody] int[] deleteIds)
        {
            try
            {
                foreach (var deleteId in deleteIds)
                {
                    
                    string query = "SELECT user_password FROM users WHERE user_id = :id";
                    var parameters = new Dictionary<string, object> { { "id", deleteId } };

                    List<Dictionary<string, object>> rawResult = await _context.ExecuteQueryAsync(query, parameters);
                    List<Dictionary<string, string>> result = rawResult.Select(d => d.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())).ToList();

                    if (result != null && result.Count == 1)
                    {
                       
                            // 删除用户记录
                            string deleteQuery = "DELETE FROM users WHERE user_id = :id";
                            var deleteParameters = new Dictionary<string, object> { { "id", deleteId} };
                            int rowsDeleted = await _context.ExecuteNonQueryAsync(deleteQuery, deleteParameters);

                            if (rowsDeleted == 0)
                            {
                                return NotFound($"User with id {deleteId} not found.");
                            }
                       
                    }
                    else
                    {
                        return NotFound($"User with id {deleteId} not found.");
                    }
                }

                return Ok(new { code = 200});
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        //POST v1/user/saveImage
        [HttpPost("saveImage")]
        public async Task<IActionResult> PostSaveImage([FromBody] ChangeImageRequest ChangeImageRequest)
        {
            try
            {
                // 构建更新 SQL 查询和参数
                string updateQuery = "UPDATE users SET icon = :icon,  delete_icon = :delete_icon WHERE user_id = :id";

                var updateParameters = new Dictionary<string, object>
                {
                    { "icon", ChangeImageRequest.icon },
                    { "delete_icon", ChangeImageRequest.delete_icon },
                    { "id", ChangeImageRequest.user_id }
                };

                // 执行更新操作
                int rowsUpdated = await _context.ExecuteNonQueryAsync(updateQuery, updateParameters);

                // 检查更新是否成功并返回相应结果
                if (rowsUpdated > 0)
                {
                    var good_response = new
                    {
                        code = 200,
                        msg = "添加成功",
                    };
                    return Ok(good_response);
                }
                else
                {
                    var good_response = new
                    {
                        code = 300,
                        msg = "用户不存在或添加失败",
                    };
                    return NotFound(good_response);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        // DELETE /v1/user/deleteImage
        [HttpDelete("deleteImage")]
        public async Task<IActionResult> DeleteImage([FromQuery] string delete_url)
        {
            try
            {
                if (string.IsNullOrEmpty(delete_url))
                {
                    return BadRequest("Delete URL cannot be null or empty.");
                }

                // 使用 HttpClient 发送删除请求到图床服务
                using (var client = new HttpClient())
                {
                    var request = new HttpRequestMessage(HttpMethod.Delete, delete_url);

                    // 发送请求
                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        // 返回成功响应
                        return Ok(new
                        {
                            code = 200,
                            msg = "Image deleted successfully from the image hosting service."
                        });
                    }
                    else
                    {
                        // 返回失败响应
                        return StatusCode((int)response.StatusCode, new
                        {
                            code = (int)response.StatusCode,
                            msg = "Failed to delete image from the image hosting service."
                        });
                    }
                }
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

