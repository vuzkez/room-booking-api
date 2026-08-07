using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RoomBookingApi.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using RoomBookingApi.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Exceptions;

namespace RoomBookingApi.Application.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration configuration,ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string GenerateJwtToken(UserApplication user,IList<RoleApplication> roles)
        {
            var expiresFromSettings = _configuration["Jwt:ExpiryInMinutes"];
            var keyFromSettings = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (expiresFromSettings == null
                || keyFromSettings == null
                || issuer == null
                || audience == null)
            {
                var settings = new List<string?>()
                {
                    expiresFromSettings,keyFromSettings,issuer, audience
                };
                throw new JwtSettingsNullException(settings);
            }

            var expires = DateTime.UtcNow.AddMinutes(Int32.Parse(expiresFromSettings));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyFromSettings));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),
                new Claim(ClaimTypes.Email,user.Email!),
                new Claim(ClaimTypes.Name,user.UserName!)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role.Name!));

            var token = new JwtSecurityToken(
                audience: audience,
                issuer: issuer,
                expires: expires,
                claims: claims,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
