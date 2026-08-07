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
using RoomBookingApi.Infrastructure.Data;

namespace RoomBookingApi.Infrastructure.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly AppDbContext _context;

        public RoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Room?> GetByIdAsync(int id)
        {
            return await _context.Rooms.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<IEnumerable<Room>> GetAllAsync()
        {
            return await _context.Rooms.ToListAsync();
        }

        public async Task AddAsync(Room room)
        {
            await _context.Rooms.AddAsync(room);
        }

        public async Task DeleteByIdAsync(int id)
        {
            var room = await _context.Rooms.FirstOrDefaultAsync(x => x.Id == id);

            if (room == null)
                throw new NotFoundException(nameof(Room), id);

            _context.Rooms.Remove(room);
        }

        public async Task UpdateAsync(Room room)
        {
            _context.Rooms.Update(room);
        }
        public async Task<(int TotalCount, List<Room> Items)> GetPagedWithFiltersAsync(RoomFilterDto filter,int page,int pageSize)
        {
            IQueryable<Room> query = _context.Rooms;

            if (filter.MinCapacity.HasValue)
                query = query.Where(r => r.Capacity >= filter.MinCapacity.Value);

            if (!string.IsNullOrEmpty(filter.Location))
                query = query.Where(r => r.Location.Contains(filter.Location.ToLower()));

            if (filter.IsActive.HasValue)
                query = query.Where(r => r.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (totalCount, items);
        }
    }
}
