// Authorization/JwtAuthorizeAttribute.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection.KeyManagement;

namespace FootballManagerBackend.Authorization
{
    public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public JwtAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var token = context.HttpContext.Request.Cookies["token"];
            var configuration = context.HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;

            var secretKey = configuration["Jwt:Key"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];


            if (token == null)
            {
                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Authorization token is missing.",
                    ContentType = "text/plain"
                };
                return;
            }

            if(secretKey == null)
            {
                context.Result = new ContentResult
                {
                    StatusCode = 401,
                    Content = "Secret Key is missing.",
                    ContentType = "text/plain"
                };
                return;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(secretKey);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
                var userRole = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
                var teamId = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.GroupSid)?.Value;
                // 检查角色权限
                if (_roles.Length > 0 && !_roles.Contains(userRole))
                {
                    context.Result = new ContentResult
                    {
                        StatusCode = 403, // Forbidden
                        Content = "You do not have the necessary role to access this resource.",
                        ContentType = "text/plain"
                    };
                    return;
                }

                // 验证通过后，将用户信息存入上下文中
                context.HttpContext.Items["UserId"] = userId;
                context.HttpContext.Items["UserRole"] = userRole;
                context.HttpContext.Items["TeamId"] = teamId;
            }
            catch
            {
                context.Result = new ContentResult
                {
                    StatusCode = 403, // Forbidden
                    Content = "You do not have the necessary role to access this resource.",
                    ContentType = "text/plain"
                };
                return;
            }
        }
    }
}
