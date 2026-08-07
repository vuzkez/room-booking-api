using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using RoomBookingApi.Application.Dto;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Entities;
using RoomBookingApi.Application.Exceptions;

namespace RoomBookingApi.Application.Services
{
    public class RoomService : IRoomService
    {
        private readonly ILogger<RoomService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public RoomService(ILogger<RoomService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }
        public async Task<RoomResponseDto> CreateAsync(CreateRoomRequestDto request)
        {
            var room = new Room()
            {
                Capacity = request.Capacity,
                Location = request.Location,
                PricePerHour = request.PricePerHour,
                RoomName = request.Name,
                IsActive = true
            };

            try
            {
                await _unitOfWork.Rooms.AddAsync(room);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding room.");
                throw;
            }

            _logger.LogInformation("Room created: {RoomName} (Id: {RoomId})", room.RoomName, room.Id);

            return new RoomResponseDto()
            {
                Id = room.Id,
                Capacity = room.Capacity,
                IsActive = room.IsActive,
                Location = room.Location,
                Name = room.RoomName,
                PricePerHour= room.PricePerHour,
            };
        }

        public async Task DeleteByIdAsync(int id)
        {
            try
            {
                await _unitOfWork.Rooms.DeleteByIdAsync(id);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding room.");
                throw;
            }
        }

        public async Task<PagedResult<RoomResponseDto>> GetAllAsync(RoomFilterDto filter, int page, int pageSize)
        {
            var (totalCount, rooms) = await _unitOfWork.Rooms
                .GetPagedWithFiltersAsync(filter, page, pageSize);

            var items = rooms.Select(r => new RoomResponseDto
            {
                Id = r.Id,
                Name = r.RoomName,
                Capacity = r.Capacity,
                Location = r.Location,
                PricePerHour = r.PricePerHour,
                IsActive = r.IsActive
            }).ToList();

            return new PagedResult<RoomResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        public async Task<RoomResponseDto> GetByIdAsync(int id)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(id);
            if (room == null)
                throw new NotFoundException(nameof(Room), id);

            return new RoomResponseDto
            {
                Id = room.Id,
                Name = room.RoomName,
                Capacity = room.Capacity,
                Location = room.Location,
                PricePerHour = room.PricePerHour,
                IsActive = room.IsActive
            };
        }

        public async Task<RoomResponseDto> UpdateByIdAsync(int id, UpdateRoomRequestDto request)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(id);
            if (room == null)
                throw new NotFoundException(nameof(Room), id);

            room.RoomName = request.Name;
            room.Capacity = request.Capacity;
            room.Location = request.Location;
            room.PricePerHour = request.PricePerHour;
            room.IsActive = request.IsActive;

            try
            {
                await _unitOfWork.Rooms.UpdateAsync(room);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room.");
                throw;
            }

            _logger.LogInformation("Room updated: {RoomName} (Id: {RoomId})", room.RoomName, room.Id);

            return new RoomResponseDto
            {
                Id = room.Id,
                Name = room.RoomName,
                Capacity = room.Capacity,
                Location = room.Location,
                PricePerHour = room.PricePerHour,
                IsActive = room.IsActive
            };
        }
    }
}
