using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;


namespace Billing.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class LoginRequest
        {
            public string ClientType { get; set; } = string.Empty; // "mobile", "bank", "admin"
            public string Username { get; set; } = string.Empty;   // basit olsun
            public string Password { get; set; } = string.Empty;
        }

        public class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
        }

        // POST: api/v1/auth/login
        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            // NOT: Bu tamamen demo amaçlı, gerçek DB kontrolü yok.
            // ClientType + Username + Password hard-coded kabul ediyoruz.

            var role = request.ClientType.ToLowerInvariant();

            // Basit bir doğrulama: şimdilik her şey kabul, ama role'ü sadece 3 tipten biri yapalım
            if (role != "mobile" && role != "bank" && role != "admin")
            {
                return BadRequest("ClientType must be one of: mobile, bank, admin.");
            }

            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, request.Username),
                new Claim(ClaimTypes.Role, role)
            };

            var expiresMinutes = int.TryParse(jwtSettings["ExpiresMinutes"], out var minutes)
                ? minutes : 60;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new LoginResponse
            {
                Token = tokenString,
                ExpiresAt = tokenDescriptor.Expires ?? DateTime.UtcNow.AddMinutes(expiresMinutes)
            });
        }
    }
}
