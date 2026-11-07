using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Application.Options;
using RoomBookingApi.Domain.Entities;

namespace RoomBookingApi.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly JwtSettings _settings;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IOptions<JwtSettings> options, ILogger<JwtService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public string GenerateJwtToken(UserApplication user, IList<RoleApplication> roles)
        {
            var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryInMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!)
            };

            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role.Name!));

            var token = new JwtSecurityToken(
                audience: _settings.Audience,
                issuer: _settings.Issuer,
                expires: expires,
                claims: claims,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}