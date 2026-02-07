using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApiInterviewStatus.Helpers;
using WebApiInterviewStatus.Models;
using WebApiInterviewStatus.Models.Auth;
using WebApiInterviewStatus.Services;

namespace WebApiInterviewStatus.Controllers.V1.Auth
{
    [EnableRateLimiting("authPolicy")]
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthController(MainModel mainModel, IConfiguration config, TokenBlacklistService blacklist) : ControllerBase
    {
        private readonly MainModel _mainModel = mainModel;
        private readonly IConfiguration _config = config;
        private readonly TokenBlacklistService _blacklist = blacklist;


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {


            // 🔍 Check use role if exist
            var existing_user_role = await _mainModel.GetRowAsync(
                "SELECT user_role_id FROM tbl_users_role WHERE user_role_id = @user_role_id",
                new { user_role_id = dto.UserRole } 
            );

            if (existing_user_role == null)
                return BadRequest(new
                {
                    success = false,
                    message = "UserRole Id Invalid"
                });

            // 🔍 Check existing user
            var existing = await _mainModel.GetRowAsync(
                $@"SELECT username, email 
                  FROM tbl_users 
                  WHERE username = @username OR email = @email",
                new { username = dto.Username, email = dto.Email }
            );

            if (existing != null)
                return BadRequest(new {
                    success = false,
                    message = "Username or Email already exists" 
                });

            // 🔐 Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 📝 Insert to DB
            var result = await _mainModel.InsertAsync(
                "tbl_users",
                new
                {
                    username = dto.Username,
                    email = dto.Email,
                    password_hash = passwordHash,
                    user_role_id = dto.UserRole,
                    full_name = dto.Fullname,
                    added_date = DateTime.UtcNow
                }
            );

            if (!(bool)result.results)
                return StatusCode(500, "Failed to register");

            return Ok(new
            {
                success = true,
                message = "User registered successfully",
                userId = result.id
            });
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {

            var user = await _mainModel.GetRowAsync(
                  @"SELECT a.user_id, a.username, a.password_hash, a.user_role_id, b.role_name
                  FROM tbl_users as a INNER JOIN tbl_users_role as b ON a.user_role_id = b.user_role_id
                  WHERE a.username = @username",
                          new { username = dto.Username }
                      );

            if (user == null)
                return Unauthorized("Invalid credentials");

            // 🔐 Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, (string)user.password_hash))
                return Unauthorized("Invalid credentials");

            // 🎟 Claims for JWT
            var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, (string)user.username),
                        new Claim("UserId", user.user_id.ToString()),
                        new Claim("UserRoleId", user.user_role_id.ToString())
                    };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return Ok(new
            {
                message = true,
                data = new {
                    userRole = user.user_role_id,
                    userRoleName = user.role_name,
                },
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expires = DateTime.UtcNow.AddHours(2)
            });
        }
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"]
                .ToString().Replace("Bearer ", "");

            _blacklist.Add(token);

            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }
}
