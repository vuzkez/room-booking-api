using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Application.Interfaces.Services;

namespace RoomBookingApi.Application.Services
{
    public class EmailCodeGeneratorService : IEmailCodeGenerator
    {
        public EmailConfirmationCode Generate(int userId)
        {
            var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString("D6");
            var expires = DateTime.UtcNow.AddMinutes(5);

            var emailCode = new EmailConfirmationCode
            {
                Code = code,
                ExpiresAt = expires,
                UserId = userId,
                IsUsed = false
            };

            return emailCode;
        }
    }
}
