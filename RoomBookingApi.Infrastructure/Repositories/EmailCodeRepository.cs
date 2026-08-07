using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RoomBookingApi.Application.Interfaces.Repositories;

namespace RoomBookingApi.Infrastructure.Repositories
{
    public class EmailCodeRepository : IEmailCodeRepository
    {
        private readonly AppDbContext _context;

        public EmailCodeRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<EmailConfirmationCode?> GetByIdAsync(int id)
        {
            return await _context.EmailCodes.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<EmailConfirmationCode>> GetAllAsync()
        {
            return await _context.EmailCodes.ToListAsync();
        }

        public async Task AddAsync(EmailConfirmationCode emailConfirmationCode)
        {
            await _context.EmailCodes.AddAsync(emailConfirmationCode);
        }

        public async Task DeleteByIdAsync(int id)
        {
            var emailCode = await _context.EmailCodes.FirstOrDefaultAsync(x => x.Id == id);

            if (emailCode == null)
                throw new NotFoundException(nameof(EmailConfirmationCode), id);

            _context.EmailCodes.Remove(emailCode);
        }

        public async Task UpdateAsync(EmailConfirmationCode emailConfirmationCode)
        {
            _context.EmailCodes.Update(emailConfirmationCode);
        }

        public async Task<EmailConfirmationCode?> GetByUserIdAndCodeAsync(int userId,string code)
        {
            return await _context.EmailCodes.FirstOrDefaultAsync(x => x.UserId == userId && x.Code == code);
        }

        public async Task<bool> HasUnusedValidCodeAsync(int userId)
        {
            return await _context.EmailCodes.AnyAsync(x => x.UserId == userId && !x.IsUsed && x.ExpiresAt > DateTime.UtcNow);
        }
    }
}
