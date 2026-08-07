using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RoomBookingApi.Application.Interfaces.Repositories;
using RoomBookingApi.Application.Interfaces.Services;
using RoomBookingApi.Domain.Enums;

namespace RoomBookingApi.Application.Services
{
    public class AvailabilityChecker : IAvailabilityChecker
    {
        private readonly IUnitOfWork _uow;

        public AvailabilityChecker(IUnitOfWork uow)
        {
            _uow = uow;
        }
        public async Task<bool> IsAvailableAsync(int roomId, DateTime start, DateTime end)
        {
            var room = await _uow.Rooms.GetByIdAsync(roomId);

            if (room == null) 
                return false;

            var hasOverlap = await _uow.Bookings.HasBookingBetweenTimeAsync(roomId, start, end);
            return !hasOverlap;
        }
    }
}
