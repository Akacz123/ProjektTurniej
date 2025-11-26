using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjektTurniej.Services
{
    public class JwtService
    {
        private readonly string _key = string.Empty;
        private readonly string _issuer = string.Empty;
        private readonly string _audience = string.Empty;
        private readonly int _expireMinutes;

        public JwtService(IConfiguration config)
        {
            _key = config["Jwt:Key"] ?? throw new Exception("Brak Jwt:Key w appsettings.json");
            _issuer = config["Jwt:Issuer"] ?? throw new Exception("Brak Jwt:Issuer w appsettings.json");
            _audience = config["Jwt:Audience"] ?? throw new Exception("Brak Jwt:Audience w appsettings.json");
            _expireMinutes = int.Parse(config["Jwt:ExpireMinutes"] ?? "60");
        }

        public string GenerateToken(int userId, string username, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("username", username),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
