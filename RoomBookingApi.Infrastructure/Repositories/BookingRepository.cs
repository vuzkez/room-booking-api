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

        public async Task AddAsync(Booking booking)
        {
            await _context.Bookings.AddAsync(booking);
        }

        public async Task UpdateAsync(Booking booking)
        {
            _context.Bookings.Update(booking);
        }

        public async Task<List<BookingResponseDto>> GetPagedByUserIdAsync(int userId, int page, int pageSize)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    RoomId = b.RoomId,
                    RoomName = b.Room.RoomName,
                    UserId = b.UserId,
                    UserName = b.User.UserName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<int> CountByUserIdAsync(int userId)
        {
            return await _context.Bookings
                .Where(b => b.UserId == userId)
                .CountAsync();
        }

        public async Task<(int TotalCount, List<BookingResponseDto> Items)> GetPagedByRoomIdWithFiltersAsync(int roomId, BookingFilterDto filter, int page, int pageSize)
        {
            IQueryable<Booking> query = _context.Bookings.AsNoTracking().Where(b => b.RoomId == roomId);

            if (filter.From.HasValue) query = query.Where(b => b.StartTime >= filter.From.Value);
            if (filter.To.HasValue) query = query.Where(b => b.EndTime <= filter.To.Value);
            if (filter.Status.HasValue) query = query.Where(b => b.Status == filter.Status.Value);
            if (filter.UserId.HasValue) query = query.Where(b => b.UserId == filter.UserId.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(b => b.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    RoomId = b.RoomId,
                    RoomName = b.Room.RoomName,
                    UserId = b.UserId,
                    UserName = b.User.UserName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            return (totalCount, items);
        }

        public async Task<BookingResponseDto?> GetByIdWithIncludesAsync(int id)
        {
            return await _context.Bookings
                .AsNoTracking()
                .Where(b => b.Id == id)
                .Select(b => new BookingResponseDto
                {
                    Id = b.Id,
                    RoomId = b.RoomId,
                    RoomName = b.Room.RoomName,
                    UserId = b.UserId,
                    UserName = b.User.UserName,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasBookingBetweenTimeAsync(int roomId, DateTime start, DateTime end)
        {
            return await _context.Bookings
                .AnyAsync(b => b.RoomId == roomId
                               && (b.Status == Status.Confirmed || b.Status == Status.Pending)
                               && b.StartTime < end
                               && b.EndTime > start);
        }
    }
}
