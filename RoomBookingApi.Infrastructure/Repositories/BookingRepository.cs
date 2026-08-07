using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Exceptions;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Domain.Enums;
using RoomBookingApi.Infrastructure.Data;

namespace RoomBookingApi.Infrastructure.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly AppDbContext _context;

        public BookingRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings.ToListAsync();
        }

        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public async Task DeleteByIdAsync(int id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id);

            if (booking == null)
                throw new NotFoundException(nameof(Booking), id);

            _context.Bookings.Remove(booking);
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public async Task<List<Booking>> GetAllByUserId(int userId)
        {
            return await _context.Bookings.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<List<Booking>> GetPagedAsync(int page, int pageSize)
        {
            return await _context.Bookings.OrderBy(x => x.StartTime).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<List<Booking>> GetPagedByUserIdAsync(int userId, int page, int pageSize)
        {
            return await _context.Bookings
                .Where(x => x.UserId == userId)
                .Include(x => x.Room)
                .Include(x => x.User)
                .OrderBy(x => x.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .CountAsync();
        }

        public async Task<(int TotalCount, List<Booking> Items)> GetPagedByRoomIdWithFiltersAsync(int roomId, BookingFilterDto filter, int page, int pageSize)
        {
            IQueryable<Booking> query = _context.Bookings
                .Where(b => b.RoomId == roomId)
                .Include(b => b.Room)
                .Include(b => b.User);

            if (filter.From.HasValue)
                query = query.Where(b => b.StartTime >= filter.From.Value);

            if (filter.To.HasValue)
                query = query.Where(b => b.EndTime <= filter.To.Value);

            if (filter.Status.HasValue)
                query = query.Where(b => b.Status == filter.Status.Value);

            if (filter.UserId.HasValue)
                query = query.Where(b => b.UserId == filter.UserId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(b => b.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (totalCount, items);
        }

        public async Task<Booking?> GetByIdWithIncludesAsync(int id)
        {
            return await _context.Bookings.Include(x => x.User).Include(x => x.Room).FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> HasBookingBetweenTimeAsync(int roomId, DateTime start, DateTime end)
        {
            return await _context.Bookings
                .AnyAsync(b => b.RoomId == roomId
                               && b.Status == Status.Confirmed
                               && b.StartTime < end
                               && b.EndTime > start);
        }
    }
}
